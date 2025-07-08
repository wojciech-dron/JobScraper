using JobScraper.Extensions;
using JobScraper.Models;
using JobScraper.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JobScraper.Logic.Common;

public abstract class DetailsScrapperBase<TScrapeCommand> : ScrapperBase, IRequestHandler<TScrapeCommand, ScrapeResponse>
    where TScrapeCommand : ScrapeCommand
{
    public DetailsScrapperBase(IOptions<AppSettings> config,
        ILogger<DetailsScrapperBase<TScrapeCommand>> logger,
        JobsDbContext dbContext) : base(config, logger, dbContext)
    { }

    public abstract Task<JobOffer> ScrapeJobDetails(JobOffer jobOffer);

    public virtual async Task<ScrapeResponse> Handle(TScrapeCommand scrape, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            Logger.LogWarning("Scraper is disabled. Please configure {DataOrigin} origin in scraper configuration.",
                DataOrigin);
            return new ScrapeResponse();
        }

        var jobs = await DbContext.JobOffers
            .Include(j => j.Company)
            .Where(j => j.Origin              == DataOrigin)
            .Where(j => j.DetailsScrapeStatus != DetailsScrapeStatus.Scraped)
            .ToListAsync(cancellationToken);

        Logger.LogInformation("Found {Count} jobs to scrape details", jobs.Count);

        foreach (var job in jobs)
        {
            try
            {
                await RetryPolicy.ExecuteAsync(async () =>
                    await ScrapeJobDetails(job));

                job.DetailsScrapeStatus = DetailsScrapeStatus.Scraped;
                job.ProcessKeywords(ScrapeConfig);

                await DbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Failed to scrape job: {JobUrl}", job.OfferUrl);
                job.DetailsScrapeStatus = DetailsScrapeStatus.Failed;

                await DbContext.SaveChangesAsync(cancellationToken);

                throw;
            }
            finally
            {
                Dispose();
            }
        }

        return new ScrapeResponse(ScrapedOffersCount: jobs.Count);
    }
}
