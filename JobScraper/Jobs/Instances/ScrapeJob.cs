using JobScraper.Common.Extensions;
using JobScraper.Logic.Common;
using JobScraper.Logic.Indeed;
using JobScraper.Logic.Jjit;
using JobScraper.Logic.NoFluffJobs;
using JobScraper.Logic.Olx;
using JobScraper.Logic.PracujPl;
using JobScraper.Logic.RocketJobs;
using JobScraper.Models;
using JobScraper.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Models;

namespace JobScraper.Jobs.Instances;

public class ScrapeJob
{
    private readonly IMediator _mediator;
    private readonly JobsDbContext _dbContext;
    private readonly ILogger<ScrapeJob> _logger;

    public ScrapeJob(IMediator mediator,
        JobsDbContext dbContext,
        ILogger<ScrapeJob> logger)
    {
        _mediator = mediator;
        _dbContext = dbContext;
        _logger = logger;
    }

    [TickerFunction("ScrapeJobs")]
    public async Task ScrapeJobs(TickerFunctionContext<string> tickerContext, CancellationToken cancellationToken)
    {
        var config = await _dbContext.ScraperConfigs.FirstOrDefaultAsync(cancellationToken);
        if (config is null)
            return;

        var sources = config.Sources.Where(x => !x.Disabled).ToArray();

        _logger.LogInformation("Scheduled scraping in progress");

        var newOffersCount = await ScrapeLists(sources);
        _logger.LogInformation("Scraped new {NewOffersCount} list jobs. Scraping details", newOffersCount);
        await ScrapeDetails(sources);

        _logger.LogInformation("Scheduled scraping completed successfully");

    }

    // TODO: Unify this methods here and in ScrapePage.razor.cs

    private async Task<int> ScrapeLists(IEnumerable<SourceConfig> sources)
    {
        var listCommands = sources
            .Where(s => !s.Disabled)
            .Select<SourceConfig, ScrapeCommand>(source => source.DataOrigin switch
            {
                DataOrigin.Indeed      => new IndeedListScraper.Command { Source = source },
                DataOrigin.JustJoinIt  => new JjitListScraper.Command { Source = source },
                DataOrigin.NoFluffJobs => new NoFluffJobsListScraper.Command { Source = source },
                DataOrigin.PracujPl    => new PracujPlListScraper.Command { Source = source },
                DataOrigin.RocketJobs  => new RocketJobsListScraper.Command { Source = source },
                DataOrigin.Olx         => new OlxListScraper.Command { Source = source },
                _                      => throw new ArgumentOutOfRangeException($"List scraping not implemented for {source}")
            }).ToArray();

        var offersCount = 0;
        for (int idx = 0; idx < listCommands.Length; idx++)
        {
            var command = listCommands[idx];
            _logger.LogInformation("Scheduled scraping list pages of source {Index}/{CommandsCount}",
                idx + 1, listCommands.Length);

            var result = await _mediator.SendWithRetry(command, logger: _logger);
            offersCount += result.ScrapedOffersCount;
        }

        return offersCount;
    }

    private async Task<int> ScrapeDetails(IEnumerable<SourceConfig> sources)
    {
        var detailsCommands = sources
            .Where(s => !s.Disabled)
            .Select<SourceConfig, ScrapeCommand?>(source => source.DataOrigin switch
            {
                DataOrigin.Indeed      => new IndeedDetailsScraper.Command { Source = source },
                DataOrigin.JustJoinIt  => new JjitDetailsScraper.Command { Source = source },
                DataOrigin.NoFluffJobs => new NoFluffJobsDetailsScraper.Command { Source = source },
                DataOrigin.RocketJobs  => new RocketJobsDetailsScraper.Command { Source = source },
                DataOrigin.PracujPl    => null,
                DataOrigin.Olx         => null,

                _ => throw new NotImplementedException($"List scraping not implemented for {source}")
            }).Where(c => c is not null)
            .ToArray();

        var offersCount = 0;
        for (int idx = 0; idx < detailsCommands.Length; idx++)
        {
            var command = detailsCommands[idx]!;
            _logger.LogInformation("Scheduled scraping details pages of source {Index}/{CommandsCount}",
                idx + 1, detailsCommands.Length);

            var result = await _mediator.SendWithRetry(command, logger: _logger);
            offersCount += result.ScrapedOffersCount;
        }

        return offersCount;
    }
}