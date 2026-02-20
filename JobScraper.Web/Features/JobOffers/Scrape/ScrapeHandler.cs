using JobScraper.Web.Common.Entities;
using JobScraper.Web.Features.AiSummary;
using JobScraper.Web.Features.JobOffers.Scrape.Logic.Common;
using JobScraper.Web.Features.JobOffers.Scrape.Logic.Indeed;
using JobScraper.Web.Features.JobOffers.Scrape.Logic.Jjit;
using JobScraper.Web.Features.JobOffers.Scrape.Logic.NoFluffJobs;
using JobScraper.Web.Features.JobOffers.Scrape.Logic.Olx;
using JobScraper.Web.Features.JobOffers.Scrape.Logic.PracujPl;
using JobScraper.Web.Features.JobOffers.Scrape.Logic.RocketJobs;
using JobScraper.Web.Modules.Extensions;
using JobScraper.Web.Modules.Persistence;
using JobScraper.Web.Modules.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;
using TickerQ.Utilities.Base;

namespace JobScraper.Web.Features.JobOffers.Scrape;

public record struct ScrapeRequest(string Owner);

public sealed partial class ScrapeHandler(
    IMediator mediator,
    UserProvider userProvider,
    JobsDbContext dbContext,
    ILogger<ScrapeHandler> logger
)
{

    public static readonly SemaphoreSlim ScrapeSemaphore = new(1, 1);

    [TickerFunction("ScrapeJobs")]
    public async Task ScrapeJobs(TickerFunctionContext<ScrapeRequest> context, CancellationToken cancellationToken)
    {
        dbContext.CurrentUserName = context.Request.Owner;
        userProvider.UserName = context.Request.Owner;

        var config = await dbContext.ScraperConfigs.FirstOrDefaultAsync(cancellationToken);
        if (config is null)
            return;

        LogScrapingInProgress(context.Request.Owner);

        var newOffersCount = await ScrapeLists(config.Sources, cancellationToken);
        if (newOffersCount == -1)
            throw new ApplicationException("Scrape semaphore is locked");

        LogScrapedNewOffersFromLists(newOffersCount);

        var detailUrls = await ScrapeDetails(config.Sources, cancellationToken);
        if (detailUrls is null)
            throw new ApplicationException("Scrape semaphore is locked");

        logger.LogInformation("Scraping completed successfully");

        await MarkOffersAndScheduleAiSummary(detailUrls);
    }

    public async Task<int> ScrapeLists(IEnumerable<SourceConfig> sources,
        CancellationToken cancellationToken = default)
    {
        using var userLogScope = logger.BeginScope("Scraping lists for user {UserName}", userProvider.UserName);

        var offersCount = 0;
        var entered = await ScrapeSemaphore.WaitAsync(TimeSpan.FromMinutes(3), cancellationToken);
        if (!entered)
            return -1;

        try
        {
            var listCommands = sources
                .Where(s => !s.Disabled)
                .Select<SourceConfig, ScrapeCommand>(source => source.DataOrigin switch
                {
                    DataOrigin.Indeed      => new IndeedListScraper.Command(source),
                    DataOrigin.JustJoinIt  => new JjitListScraper.Command(source),
                    DataOrigin.NoFluffJobs => new NoFluffJobsListScraper.Command(source),
                    DataOrigin.PracujPl    => new PracujPlListScraper.Command(source),
                    DataOrigin.RocketJobs  => new RocketJobsListScraper.Command(source),
                    DataOrigin.Olx         => new OlxListScraper.Command(source),
                    _                      => throw new ArgumentOutOfRangeException($"List scraping not implemented for {source}"),
                }).ToArray();

            for (var idx = 0; idx < listCommands.Length; idx++)
            {
                var command = listCommands[idx];
                LogScrapingListPages(idx + 1, listCommands.Length);

                var result = await mediator.SendWithRetry(command, logger, cancellationToken: cancellationToken);
                offersCount += result.ScrapedOffersCount;
            }
        }
        finally
        {
            ScrapeSemaphore.Release();
        }

        return offersCount;
    }

    public async Task<string[]?> ScrapeDetails(IEnumerable<SourceConfig> sources,
        CancellationToken cancellationToken = default)
    {
        using var userLogScope = logger.BeginScope("Scraping details for user {UserName}", userProvider.UserName);

        var offersScraped = new List<string>();
        var entered = await ScrapeSemaphore.WaitAsync(TimeSpan.FromMinutes(10), cancellationToken);
        if (!entered)
            return null;

        try
        {
            var detailsCommands = sources
                .Where(s => !s.Disabled)
                .Select<SourceConfig, ScrapeCommand?>(source => source.DataOrigin switch
                {
                    DataOrigin.Indeed      => new IndeedDetailsScraper.Command(source),
                    DataOrigin.JustJoinIt  => new JjitDetailsScraper.Command(source),
                    DataOrigin.NoFluffJobs => new NoFluffJobsDetailsScraper.Command(source),
                    DataOrigin.RocketJobs  => new RocketJobsDetailsScraper.Command(source),
                    DataOrigin.PracujPl    => null,
                    DataOrigin.Olx         => null,

                    _ => throw new NotImplementedException($"List scraping not implemented for {source}"),
                }).Where(c => c is not null)
                .ToArray();

            for (var idx = 0; idx < detailsCommands.Length; idx++)
            {
                var command = detailsCommands[idx]!;
                LogScrapingDetails(idx + 1, detailsCommands.Length);

                var result = await mediator.SendWithRetry(command, logger, cancellationToken: cancellationToken);
                offersScraped.AddRange(result.OffersUrls);
            }
        }
        finally
        {
            ScrapeSemaphore.Release();
        }

        return offersScraped.ToArray();
    }

    private async Task MarkOffersAndScheduleAiSummary(string[] offerUrls)
    {
        if (dbContext.AiSummaryConfigs.All(x => x.AiSummaryEnabled == false))
            return;

        // mark offers to ai summary
        var offers = await dbContext.UserOffers
            .Where(uo => offerUrls.Contains(uo.OfferUrl))
            .ToArrayAsync();

        foreach (var offer in offers)
            offer.AiSummaryStatus = AiSummaryStatus.Marked;

        dbContext.ScheduleAiSummary(DateTime.UtcNow.AddMinutes(1));

        await dbContext.SaveChangesAsync();
    }

    [LoggerMessage(LogLevel.Information, "Scraping list pages of source {index}/{commandsCount}")]
    partial void LogScrapingListPages(int index, int commandsCount);

    [LoggerMessage(LogLevel.Information, "Scraping details pages of source {index}/{commandsCount}")]
    partial void LogScrapingDetails(int index, int commandsCount);

    [LoggerMessage(LogLevel.Information, "Scraped new {newOffersCount} list jobs. Scraping details")]
    partial void LogScrapedNewOffersFromLists(int newOffersCount);

    [LoggerMessage(LogLevel.Information, "Scraping in progress for user {UserName}")]
    partial void LogScrapingInProgress(string UserName);
}
