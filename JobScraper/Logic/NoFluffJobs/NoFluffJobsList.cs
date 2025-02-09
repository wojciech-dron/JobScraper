using Cocona;
using JobScraper.Logic.Common;
using MediatR;

namespace JobScraper.Logic.NoFluffJobs;

public class NoFluffJobsList
{
    public record Scrape : IRequest;

    internal class Handler : IRequestHandler<Scrape>
    {
        private readonly NoFluffJobsListScraper _scraper;
        private readonly ILogger<Handler> _logger;
        private readonly IMediator _mediator;

        public Handler(NoFluffJobsListScraper scraper, ILogger<Handler> logger, IMediator mediator)
        {
            _scraper = scraper;
            _logger = logger;
            _mediator = mediator;
        }

        [Command("nofluffjobs-list")]
        public async Task Handle(Scrape? scrape = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Scraping NoFluffJobs jobs list...");

            await foreach (var jobs in _scraper.ScrapeJobs().WithCancellation(cancellationToken))
            {
                _logger.LogInformation("Syncing NoFluffJobs jobs...");
                await _mediator.Send(new SyncJobsFromList.Command(jobs), cancellationToken);
            }
        }
    }
}