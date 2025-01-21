using Cocona;
using JobScraper.Models;
using JobScraper.Persistence;
using JobScraper.Scrapers;
using JobScraper.Scrapers.JustJoinIt;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JobScraper.Logic;

public class JjitDetails
{
    public record Scrape : IRequest;

    public class Handler : IRequestHandler<Scrape>
    {
        private readonly JjitDetailsScraper _scrapper;
        private readonly JobsDbContext _dbContext;
        private readonly ILogger<Handler> _logger;

        public Handler(JjitDetailsScraper scrapper, JobsDbContext dbContext, ILogger<Handler> logger)
        {
            _scrapper = scrapper;
            _dbContext = dbContext;
            _logger = logger;
        }

        [Command("jjit-details")]
        public async Task Handle(Scrape? scrape = null, CancellationToken cancellationToken = default)
        {
            var jobs = await _dbContext.JobOffers
                .Include(j => j.Company)
                .Where(j => j.Origin == DataOrigin.JustJoinIt)
                .Where(j => j.DetailsScrapeStatus != DetailsScrapeStatus.Scraped)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Found {Count} jobs to scrape details", jobs.Count);

            var addCount = 0;
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