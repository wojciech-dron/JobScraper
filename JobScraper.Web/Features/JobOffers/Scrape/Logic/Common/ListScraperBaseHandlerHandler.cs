using JobScraper.Web.Common.Entities;
using JobScraper.Web.Features.JobOffers.Scrape.Logic.Extensions;
using JobScraper.Web.Modules.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog.Context;

namespace JobScraper.Web.Features.JobOffers.Scrape.Logic.Common;

public abstract partial class ListScraperBaseHandler<TScrapeCommand>(
    IOptions<AppSettings> config,
    ILogger<ListScraperBaseHandler<TScrapeCommand>> logger,
    JobsDbContext dbContext
) : ScrapperBaseHandler(config, logger, dbContext), IRequestHandler<TScrapeCommand, ScrapeResponse>
    where TScrapeCommand : ScrapeCommand
{
    public async ValueTask<ScrapeResponse> Handle(TScrapeCommand scrape, CancellationToken cancellationToken = default)
    {
        using var userNameScope = LogContext.PushProperty("UserName", DbContext.CurrentUserName);
        using var dataOriginScope = LogContext.PushProperty("DataOrigin", DataOrigin);

        if (!IsEnabled)
        {
            Logger.LogWarning("Scraper is disabled. Please configure {DataOrigin} origin in scraper configuration",
                DataOrigin);

            return new ScrapeResponse();
        }

        if (scrape.Source.DataOrigin != DataOrigin)
            throw new InvalidOperationException("Source data origin does not match scraper type.");

        if (string.IsNullOrEmpty(scrape.Source.SearchUrl))
            throw new InvalidOperationException($"Search url is empty. Please provide a valid search url for {DataOrigin} origin");

        LogScrapingJobsList(Logger, DataOrigin);

        var userOffersUrls = new List<string>();

        await foreach (var jobs in ScrapeJobs(scrape.Source).WithCancellation(cancellationToken))
        {
            LogSyncingJobs(Logger, DataOrigin);
            var newUserOffers = await SyncJobsFromList(jobs, cancellationToken);

            userOffersUrls.AddRange(newUserOffers);
        }

        Dispose();

        return new ScrapeResponse([.. userOffersUrls]);
    }

    public abstract IAsyncEnumerable<List<JobOffer>> ScrapeJobs(SourceConfig sourceConfig);

    /// <returns>Added new user offers</returns>
    public async Task<string[]> SyncJobsFromList(List<JobOffer> jobs, CancellationToken cancellationToken)
    {
        if (jobs.Count == 0)
            return [];

        var companies = jobs
            .Select(j => j.CompanyName)
            .Where(c => c is not null)
            .Distinct()
            .Cast<string>()
            .ToArray();

        await AddNewCompanies(companies, cancellationToken);
        var trackedJobs = await AddNewJobOffers(jobs, cancellationToken);
        return await AddNewUserOffers(trackedJobs, cancellationToken);
    }

    private async Task AddNewCompanies(string[] companyNames, CancellationToken cancellationToken)
    {
        // to avoid race condition
        await using var transaction = await DbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var existingKeys = await DbContext.Companies
                .Where(c => companyNames.Contains(c.Name)) // TODO: check if contains work
                .Select(c => c.Name)
                .ToArrayAsync(cancellationToken);

            var companiesToAdd = companyNames
                .Except(existingKeys)
                .Select(name => new Company
                {
                    Name = name,
                }).ToArray();

            LogSavingNewCompanies(Logger, companiesToAdd.Length);

            await DbContext.Companies.AddRangeAsync(companiesToAdd, cancellationToken);
            await DbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<List<JobOffer>> AddNewJobOffers(List<JobOffer> jobs, CancellationToken cancellationToken)
    {
        var keys = jobs.Select(jo => jo.OfferUrl);

        // keep this in dbContext to avoid creation of duplicate job offers
        var existingJobs = await DbContext.JobOffers
            .Where(j => keys.Contains(j.OfferUrl))
            .ToArrayAsync(cancellationToken);

        var existingOfferUrls = existingJobs.Select(jo => jo.OfferUrl);

        var jobsToAdd = jobs
            .Where(jo => !existingOfferUrls.Contains(jo.OfferUrl))
            .ToList();

        await DbContext.AddRangeAsync(jobsToAdd, cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);

        // Add existing tracked jobs to the list and return
        jobsToAdd.AddRange(existingJobs);
        return jobsToAdd;
    }

    private async Task<string[]> AddNewUserOffers(List<JobOffer> jobs, CancellationToken cancellationToken)
    {
        var keys = jobs.Select(jo => jo.OfferUrl);

        // keep this in dbContext to avoid creation of duplicate job offers
        var existingUserOffers = await DbContext.UserOffers
            .Where(j => keys.Contains(j.OfferUrl))
            .ToArrayAsync(cancellationToken);

        var existingOfferUrls = existingUserOffers.Select(jo => jo.OfferUrl);

        var userOffersToAdd = jobs
            .Where(jo => !existingOfferUrls.Contains(jo.OfferUrl))
            .Select(jo => new UserOffer(jo).ProcessKeywords(ScrapeConfig))
            .ToArray();

        LogSavingUserOffers(Logger, userOffersToAdd.Length);

        await DbContext.AddRangeAsync(userOffersToAdd, cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);

        return userOffersToAdd.Select(x => x.OfferUrl).ToArray();
    }

    [LoggerMessage(LogLevel.Information, "Scraping {dataOrigin} jobs list...")]
    static partial void LogScrapingJobsList(ILogger<ScrapperBaseHandler> logger, DataOrigin dataOrigin);

    [LoggerMessage(LogLevel.Information, "Syncing {dataOrigin} jobs...")]
    static partial void LogSyncingJobs(ILogger<ScrapperBaseHandler> logger, DataOrigin dataOrigin);

    [LoggerMessage(LogLevel.Information, "Saving {jobsCount} new user offers")]
    static partial void LogSavingUserOffers(ILogger<ScrapperBaseHandler> logger, int jobsCount);
    [LoggerMessage(LogLevel.Information, "Saving {CompaniesCount} new companies")]
    static partial void LogSavingNewCompanies(ILogger<ScrapperBaseHandler> logger, int CompaniesCount);
}
