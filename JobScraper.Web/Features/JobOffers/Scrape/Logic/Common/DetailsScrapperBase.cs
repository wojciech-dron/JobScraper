using JobScraper.Web.Common.Entities;
using JobScraper.Web.Features.JobOffers.Scrape.Logic.Extensions;
using JobScraper.Web.Modules.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JobScraper.Web.Features.JobOffers.Scrape.Logic.Common;

public abstract class DetailsScrapperBase<TScrapeCommand> : ScrapperBase, IRequestHandler<TScrapeCommand, ScrapeResponse>
    where TScrapeCommand : ScrapeCommand
{
    public DetailsScrapperBase(IOptions<AppSettings> config,
        ILogger<DetailsScrapperBase<TScrapeCommand>> logger,
        JobsDbContext dbContext) : base(config, logger, dbContext)
    { }

    public virtual async ValueTask<ScrapeResponse> Handle(TScrapeCommand scrape, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            Logger.LogWarning("Scraper is disabled. Please configure {DataOrigin} origin in scraper configuration.",
                DataOrigin);
            return new ScrapeResponse();
        }

        var userOffers = await DbContext.UserOffers
            .Include(j => j.Details.Company) // TODO: check if join contains query filter
            .Where(j => j.Details.Origin              == DataOrigin)
            .Where(j => j.Details.DetailsScrapeStatus != DetailsScrapeStatus.Scraped)
            .ToListAsync(cancellationToken);

        Logger.LogInformation("Found {Count} jobs to scrape details", userOffers.Count);

        foreach (var userOffer in userOffers)
        {
            var jobOffer = userOffer.Details;
            using var offerUrlScope = Logger.BeginScope("OfferUrl: {OfferUrl}", jobOffer.OfferUrl);

            try
            {
                await RetryPolicy.ExecuteAsync(async () =>
                    await ScrapeJobDetails(jobOffer));

                jobOffer.DetailsScrapeStatus = DetailsScrapeStatus.Scraped;

                userOffer.ProcessKeywords(ScrapeConfig);

                await DbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Failed to scrape job: {JobUrl}", jobOffer.OfferUrl);
                jobOffer.DetailsScrapeStatus = DetailsScrapeStatus.Failed;

                await DbContext.SaveChangesAsync(cancellationToken);

                throw;
            }
            finally
            {
                Dispose();
            }
        }

        return new ScrapeResponse(ScrapedOffersCount: userOffers.Count);
    }

    public abstract Task<JobOffer> ScrapeJobDetails(JobOffer jobOffer);
}
