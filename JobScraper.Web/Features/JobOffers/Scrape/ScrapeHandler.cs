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
using Mediator;
using Microsoft.EntityFrameworkCore;
using TickerQ.Utilities.Base;

namespace JobScraper.Web.Features.JobOffers.Scrape;

public class ScrapeHandler
{
    private readonly JobsDbContext _dbContext;
    private readonly ILogger<ScrapeHandler> _logger;
    private readonly IMediator _mediator;

    public ScrapeHandler(IMediator mediator,
        JobsDbContext dbContext,
        ILogger<ScrapeHandler> logger)
    {
        _mediator = mediator;
        _dbContext = dbContext;
        _logger = logger;
    }

    [TickerFunction("ScrapeJobs")]
    public async Task ScrapeJobs(CancellationToken cancellationToken)
    {
        var config = await _dbContext.ScraperConfigs.FirstOrDefaultAsync(cancellationToken);
        if (config is null)
            return;

        _logger.LogInformation("Scheduled scraping in progress");

        var newOffersCount = await ScrapeLists(config.Sources);
        _logger.LogInformation("Scraped new {NewOffersCount} list jobs. Scraping details", newOffersCount);
        await ScrapeDetails(config.Sources);

        _logger.LogInformation("Scheduled scraping completed successfully");

    }

    public async Task<int> ScrapeLists(IEnumerable<SourceConfig> sources)
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

        var offersCount = 0;
        for (var idx = 0; idx < listCommands.Length; idx++)
        {
            var command = listCommands[idx];
            _logger.LogInformation("Scheduled scraping list pages of source {Index}/{CommandsCount}",
                idx + 1,
                listCommands.Length);

            var result = await _mediator.SendWithRetry(command, _logger);
            offersCount += result.ScrapedOffersCount;
        }

        return offersCount;
    }

    public async Task<int> ScrapeDetails(IEnumerable<SourceConfig> sources)
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

        var offersCount = 0;
        for (var idx = 0; idx < detailsCommands.Length; idx++)
        {
            var command = detailsCommands[idx]!;
            _logger.LogInformation("Scheduled scraping details pages of source {Index}/{CommandsCount}",
                idx + 1,
                detailsCommands.Length);

            var result = await _mediator.SendWithRetry(command, _logger);
            offersCount += result.ScrapedOffersCount;
        }

        return offersCount;
    }
}
