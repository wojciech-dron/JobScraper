using Cocona;
using JobScraper.Models;
using JobScraper.Persistence;
using JobScraper.Scrapers;
using JobScraper.Scrapers.Indeed;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JobScraper.Logic;

public class IndeedDetails
{
    public record Scrape : IRequest;

    public class Handler : IRequestHandler<Scrape>
    {
        private readonly IndeedDetailsScraper _scrapper;
        private readonly JobsDbContext _dbContext;
        private readonly ILogger<Handler> _logger;

        public Handler(IndeedDetailsScraper scrapper, JobsDbContext dbContext, ILogger<Handler> logger)
        {
            _scrapper = scrapper;
            _dbContext = dbContext;
            _logger = logger;
        }

        [Command("indeed-details")]
        public async Task Handle(Scrape? scrape = null, CancellationToken cancellationToken = default)
        {
            var jobs = await _dbContext.JobOffers
                .Include(j => j.Company)
                .Where(j => j.Origin == DataOrigin.Indeed)
                .Where(j => j.DetailsScrapeStatus != DetailsScrapeStatus.Scraped)
                .ToListAsync(cancellationToken);

            foreach (var job in jobs)
            {
                try
                {
                    await ScrapperBase.RetryPolicy.ExecuteAsync(async () =>
                        await _scrapper.ScrapeJobDetails(job));

                    await _scrapper.DisposeAsync();

                    job.DetailsScrapeStatus = DetailsScrapeStatus.Scraped;

                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to scrape job: {JobUrl}", job.OfferUrl);
                    job.DetailsScrapeStatus = DetailsScrapeStatus.Failed;

                    await _dbContext.SaveChangesAsync(cancellationToken);

                    throw;
                }

            }
        }
    }
}