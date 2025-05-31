using Cocona;
using JobScraper.Common.Extensions;
using JobScraper.Logic.Common;
using JobScraper.Logic.Indeed;
using JobScraper.Logic.Jjit;
using JobScraper.Logic.NoFluffJobs;
using JobScraper.Logic.PracujPl;
using JobScraper.Logic.RocketJobs;
using JobScraper.Models;
using MediatR;
using Microsoft.Extensions.Options;

namespace JobScraper.Logic;

public class ScrapePipeline
{
    public class Request : IRequest;

    public class Handler : IRequestHandler<Request>
    {
        private readonly IMediator _mediator;
        private readonly ScraperConfig _config;
        private readonly ILogger<Handler> _logger;

        public Handler(IMediator mediator,
            IOptions<ScraperConfig> config,
            ILogger<Handler> logger)
        {
            _mediator = mediator;
            _config = config.Value;
            _logger = logger;
        }

        public async Task Handle(Request? request = null, CancellationToken cancellationToken = default)
        {
            var enabledOrigins = _config.GetEnabledOrigins();

            // list
            _logger.LogInformation("Scraping lists for origins: {EnabledOrigins}", string.Join(", ", enabledOrigins));
            var listCommands = enabledOrigins.Select<DataOrigin, ScrapeCommand>(origin => origin switch
            {
                DataOrigin.Indeed      => new IndeedListScraper.Command(),
                DataOrigin.JustJoinIt  => new JjitListScraper.Command(),
                DataOrigin.NoFluffJobs => new NoFluffJobsListScraper.Command(),
                DataOrigin.PracujPl    => new PracujPlListScraper.Command(),
                DataOrigin.RocketJobs  => new RocketJobsListScraper.Command(),

                _ => throw new NotImplementedException($"List scraping not implemented for {origin}")

            });
            await Task.WhenAll(listCommands.Select(c => _mediator.SendWithRetry(c, cancellationToken)));

            // details
            _logger.LogInformation("Scraping details for origins: {EnabledOrigins}", string.Join(", ", enabledOrigins));
            var detailsCommands = enabledOrigins.Select<DataOrigin, ScrapeCommand?>(origin => origin switch
            {
                DataOrigin.Indeed      => new IndeedDetailsScraper.Command(),
                DataOrigin.JustJoinIt  => new JjitDetailsScraper.Command(),
                DataOrigin.NoFluffJobs => new NoFluffJobsDetailsScraper.Command(),
                DataOrigin.PracujPl    => null,
                DataOrigin.RocketJobs  => new RocketJobsDetailsScraper.Command(),

                _ => throw new NotImplementedException($"List scraping not implemented for {origin}")
            });
            await Task.WhenAll(detailsCommands
                .Where(c => c is not null)
                .Select(c => _mediator.SendWithRetry(c!, cancellationToken)
            ));


            _logger.LogInformation("Scraping completed successfully");
        }
    }
}