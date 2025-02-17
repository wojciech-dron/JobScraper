using Cocona;
using JobScraper.Logic.Common;
using MediatR;

namespace JobScraper.Logic.Indeed;

public class IndeedList
{
    public record Scrape : IRequest;

    public class Handler : IRequestHandler<Scrape>
    {
        private readonly IndeedListScraper _list;
        private readonly ILogger<Handler> _logger;
        private readonly IMediator _mediator;

        public Handler(IndeedListScraper list, ILogger<Handler> logger, IMediator mediator)
        {
            _list = list;
            _logger = logger;
            _mediator = mediator;
        }

        [Command("indeed-list")]
        public async Task Handle(Scrape? scrape = null, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Scraping Indeed jobs list...");
                await foreach (var jobs in _list.ScrapeJobs().WithCancellation(cancellationToken))
                {
                    _logger.LogInformation("Syncing {JobsCount} scraped jobs...", jobs.Count);
                    await _mediator.Send(new SyncJobsFromList.Command(jobs), cancellationToken);
                }
            }
            catch (ApplicationException e)
            {
                _logger.LogError(e, "Error occured during scaping indeed." +
                    " Probably retry exceeded after cloudflare human verification");
            }
        }
    }
}