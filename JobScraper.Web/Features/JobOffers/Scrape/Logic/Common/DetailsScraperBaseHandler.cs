using JobScraper.Web.Common.Entities;
using JobScraper.Web.Features.JobOffers.Scrape.Logic.Extensions;
using JobScraper.Web.Modules.Extensions;
using JobScraper.Web.Modules.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog.Context;

namespace JobScraper.Web.Features.JobOffers.Scrape.Logic.Common;

public abstract partial class DetailsScraperBaseHandler<TScrapeCommand>(
    IOptions<AppSettings> config,
    ILogger<DetailsScraperBaseHandler<TScrapeCommand>> logger,
    JobsDbContext dbContext
) : ScraperBaseHandler(config, logger, dbContext), IRequestHandler<TScrapeCommand, ScrapeResponse>
    where TScrapeCommand : ScrapeDetailsCommand
{
    public async ValueTask<ScrapeResponse> Handle(TScrapeCommand command, CancellationToken cancellationToken = default)
    {
        ScrapeConfig = DbContext.ScraperConfigs.First();
        SourceConfig = command.Source;

        using var userNameScope = LogContext.PushProperty("UserName", DbContext.CurrentUserName);
        using var dataOriginScope = LogContext.PushProperty("DataOrigin", Origin);

        if (command.Source.Disabled)
        {
            Logger.LogWarning("Scraper is disabled. Please configure {DataOrigin} origin in scraper configuration", Origin);
            return new ScrapeResponse();
        }

        var statuses = command.StatusesToScrape;
        var offerUrls = command.OfferUrls;
        var userOffers = await DbContext.UserOffers
            .Include(j => j.Details.Company)
            .Where(j => j.Details.Company != null)
            .Where(j => j.Details.Origin  == Origin)
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
                LogScrapingJobDetails(Logger, Origin, jobOffer.OfferUrl);

                await ScrapeJobDetails(jobOffer);

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
    static partial void LogFoundJobsToScrapeDetails(ILogger<ScraperBaseHandler> logger, int count);

    [LoggerMessage(LogLevel.Information, "Scraping {dataOrigin} job details for {offerUrl}")]
    static partial void LogScrapingJobDetails(ILogger<ScraperBaseHandler> logger, string dataOrigin, string offerUrl);
}
