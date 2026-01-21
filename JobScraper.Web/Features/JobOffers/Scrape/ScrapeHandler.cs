using JobScraper.Web.Common.Entities;
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

public sealed class ScrapeHandler
{
    private readonly JobsDbContext _dbContext;
    private readonly ILogger<ScrapeHandler> _logger;
    private readonly IMediator _mediator;
    private readonly UserProvider _userProvider;

    public static readonly SemaphoreSlim ScrapeSemaphore = new(1, 1);

    public ScrapeHandler(IMediator mediator,
        UserProvider userProvider,
        JobsDbContext dbContext,
        ILogger<ScrapeHandler> logger)
    {
        _mediator = mediator;
        _userProvider = userProvider;
        _dbContext = dbContext;
        _logger = logger;
    }

    [TickerFunction("ScrapeJobs")]
    public async Task ScrapeJobs(TickerFunctionContext<ScrapeRequest> context, CancellationToken cancellationToken)
    {
        _dbContext.CurrentUserName = context.Request.Owner;
        _userProvider.UserName = context.Request.Owner;

        var config = await _dbContext.ScraperConfigs.FirstOrDefaultAsync(cancellationToken);
        if (config is null)
            return;

        _logger.LogInformation("Scraping in progress for user {UserName}", context.Request.Owner);

        var newOffersCount = await ScrapeLists(config.Sources, cancellationToken);
        if (newOffersCount == -1)
            throw new ApplicationException("Scrape semaphore is locked");

        _logger.LogInformation("Scraped new {NewOffersCount} list jobs. Scraping details",
            newOffersCount);

        var detailsCount = await ScrapeDetails(config.Sources, cancellationToken);
        if (detailsCount == -1)
            throw new ApplicationException("Scrape semaphore is locked");

        _logger.LogInformation("Scraping completed successfully");
    }

    public async Task<int> ScrapeLists(IEnumerable<SourceConfig> sources,
        CancellationToken cancellationToken = default)
    {
        using var userLogScope = _logger.BeginScope("Scraping lists for user {UserName}", _userProvider.UserName);

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
                _logger.LogInformation("Scraping list pages of source {Index}/{CommandsCount}",
                    idx + 1,
                    listCommands.Length);

                var result = await _mediator.SendWithRetry(command, _logger, cancellationToken: cancellationToken);
                offersCount += result.ScrapedOffersCount;
            }
        }
        finally
        {
            ScrapeSemaphore.Release();
        }

        return offersCount;
    }

    public async Task<int> ScrapeDetails(IEnumerable<SourceConfig> sources,
        CancellationToken cancellationToken = default)
    {
        using var userLogScope = _logger.BeginScope("Scraping details for user {UserName}", _userProvider.UserName);

        var offersCount = 0;
        var entered = await ScrapeSemaphore.WaitAsync(TimeSpan.FromMinutes(10), cancellationToken);
        if (!entered)
            return -1;

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
                _logger.LogInformation("Scraping details pages of source {Index}/{CommandsCount}",
                    idx + 1,
                    detailsCommands.Length);

                var result = await _mediator.SendWithRetry(command, _logger, cancellationToken: cancellationToken);
                offersCount += result.ScrapedOffersCount;
            }
        }
        finally
        {
            ScrapeSemaphore.Release();
        }

        return offersCount;
    }
}
