using JobScraper.Web.Common.Entities;
using JobScraper.Web.Features.JobOffers.Scrape.Logic.Extensions;
using JobScraper.Web.Modules.Extensions;
using JobScraper.Web.Modules.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog.Context;

namespace JobScraper.Web.Features.JobOffers.Scrape.Logic.Common;

public abstract partial class DetailsScrapperBase<TScrapeCommand> : ScrapperBase, IRequestHandler<TScrapeCommand, ScrapeResponse>
    where TScrapeCommand : ScrapeDetailsCommand
{
    public DetailsScrapperBase(IOptions<AppSettings> config,
        ILogger<DetailsScrapperBase<TScrapeCommand>> logger,
        JobsDbContext dbContext) : base(config, logger, dbContext)
    { }

    public virtual async ValueTask<ScrapeResponse> Handle(TScrapeCommand command, CancellationToken cancellationToken = default)
    {
        using var userNameScope = LogContext.PushProperty("UserName", DbContext.CurrentUserName);
        using var dataOriginScope = LogContext.PushProperty("DataOrigin", DataOrigin);

        if (!IsEnabled)
        {
            Logger.LogWarning("Scraper is disabled. Please configure {DataOrigin} origin in scraper configuration",
                DataOrigin);

            return new ScrapeResponse();
        }

        var statuses = command.StatusesToScrape;
        var offerUrls = command.OfferUrls;
        var userOffers = await DbContext.UserOffers
            .Include(j => j.Details.Company) // TODO: check if join contains query filter
            .Where(j => j.Details.Origin == DataOrigin)
            .Where(j => statuses.Any(s => s == j.Details.DetailsScrapeStatus))
            .WhereIf(offerUrls.Length > 0, j => offerUrls.Contains(j.OfferUrl))
            .ToListAsync(cancellationToken);

        LogFoundJobsToScrapeDetails(Logger, userOffers.Count);

        foreach (var userOffer in userOffers)
        {
            var jobOffer = userOffer.Details;
            using var offerUrlScope = LogContext.PushProperty("OfferUrl", jobOffer.OfferUrl);

            try
            {
                LogScrapingJobDetails(Logger, DataOrigin, jobOffer.OfferUrl);

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
            }
            finally
            {
                Dispose();
            }
        }

        return new ScrapeResponse([..userOffers.Select(uo => uo.OfferUrl)]);
    }

    public abstract Task<JobOffer> ScrapeJobDetails(JobOffer jobOffer);

    [LoggerMessage(LogLevel.Information, "Found {count} jobs to scrape details")]
    static partial void LogFoundJobsToScrapeDetails(ILogger<ScrapperBase> logger, int count);

    [LoggerMessage(LogLevel.Information, "Scraping {dataOrigin} job details for {offerUrl}")]
    static partial void LogScrapingJobDetails(ILogger<ScrapperBase> logger, DataOrigin dataOrigin, string offerUrl);
}
