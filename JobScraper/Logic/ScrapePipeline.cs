using Cocona;
using JobScraper.Extensions;
using MediatR;

namespace JobScraper.Logic;

public class ScrapePipeline
{
    public class Request : IRequest;

    public class Handler : IRequestHandler<Request>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<Handler> _logger;

        public Handler(IMediator mediator,
            ILogger<Handler> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [PrimaryCommand]
        public async Task Handle(Request? request = null, CancellationToken cancellationToken = default)
        {
            // step 1
            _logger.LogInformation("Scraping all lists...");
            await Task.WhenAll([
                _mediator.SendWithRetry(new IndeedList.Scrape(), cancellationToken),
                _mediator.SendWithRetry(new JjitList.Scrape(), cancellationToken),
            ]);

            // step 2
            _logger.LogInformation("Scraping all details...");
            await Task.WhenAll([
                _mediator.SendWithRetry(new IndeedDetails.Scrape(), cancellationToken),
            ]);

            _logger.LogInformation("Scraping completed successfully");
        }
    }
}