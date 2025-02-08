using Cocona;
using JobScraper.Logic.Common;
using MediatR;

namespace JobScraper.Logic.Jjit;

public class JjitList
{
    public record Scrape : IRequest;

    internal class Handler : IRequestHandler<Scrape>
    {
        private readonly JjitListScraper _scraper;
        private readonly ILogger<Handler> _logger;
        private readonly IMediator _mediator;

        public Handler(JjitListScraper scraper, ILogger<Handler> logger, IMediator mediator)
        {
            _scraper = scraper;
            _logger = logger;
            _mediator = mediator;
        }

        [Command("jjit-list")]
        public async Task Handle(Scrape? scrape = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Scraping Just-join.it jobs list...");

            await foreach (var jobs in _scraper.ScrapeJobs().WithCancellation(cancellationToken))
            {
                _logger.LogInformation("Syncing Just-join.it jobs...");
                await _mediator.Send(new SyncJobsFromList.Command(jobs), cancellationToken);
            }
        }
    }
}