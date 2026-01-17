using JobScraper.Web.Common.Entities;
using JobScraper.Web.Features.JobOffers.Scrape.Logic.Extensions;
using JobScraper.Web.Modules.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JobScraper.Web.Features.JobOffers.Scrape.Logic.Common;

public abstract class ListScraperBase<TScrapeCommand> : ScrapperBase, IRequestHandler<TScrapeCommand, ScrapeResponse>
    where TScrapeCommand : ScrapeCommand
{
    public ListScraperBase(IOptions<AppSettings> config,
        ILogger<ListScraperBase<TScrapeCommand>> logger,
        JobsDbContext dbContext) : base(config, logger, dbContext)
    { }

    public async ValueTask<ScrapeResponse> Handle(TScrapeCommand scrape, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            Logger.LogWarning("Scraper is disabled. Please configure {DataOrigin} origin in scraper configuration.",
                DataOrigin);
            return new ScrapeResponse(ScrapedOffersCount: 0);
        }

        if (scrape.Source.DataOrigin != DataOrigin)
            throw new InvalidOperationException("Source data origin does not match scraper type.");

        if (string.IsNullOrEmpty(scrape.Source.SearchUrl))
            throw new InvalidOperationException($"Search url is empty. Please provide a valid search url for {DataOrigin} origin");

        Logger.LogInformation("Scraping {DataOrigin} jobs list...", DataOrigin);

        var newJobsCount = 0;

        await foreach (var jobs in ScrapeJobs(scrape.Source).WithCancellation(cancellationToken))
        {
            Logger.LogInformation("Syncing {DataOrigin} jobs...", DataOrigin);
            newJobsCount += await SyncJobsFromList(jobs, cancellationToken);
        }

        Dispose();

        return new ScrapeResponse(ScrapedOffersCount: newJobsCount);
    }

    public abstract IAsyncEnumerable<List<JobOffer>> ScrapeJobs(SourceConfig sourceConfig);

    public async Task<int> SyncJobsFromList(List<JobOffer> jobs, CancellationToken cancellationToken)
    {
        if (jobs.Count == 0)
            return 0;

        var companies = jobs
            .Select(j => j.CompanyName)
            .Where(c => c is not null)
            .Distinct()
            .Cast<string>()
            .ToArray();

        await AddNewCompanies(companies, cancellationToken);
        await AddNewJobOffers(jobs, cancellationToken);
        return await AddNewUserOffers(jobs, cancellationToken);
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

            Logger.LogInformation("Saving {CompaniesCount} new companies", companiesToAdd.Length);

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

    private async Task<int> AddNewJobOffers(List<JobOffer> jobs, CancellationToken cancellationToken)
    {
        var keys = jobs.Select(jo => jo.OfferUrl);

        var existingJobs = await DbContext.JobOffers
            .Where(j => keys.Contains(j.OfferUrl))
            .Select(j => j.OfferUrl)
            .ToArrayAsync(cancellationToken);

        var jobsToAdd = jobs
            .Where(jo => !existingJobs.Contains(jo.OfferUrl))
            .ToArray();

        await DbContext.AddRangeAsync(jobsToAdd, cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);

        return jobsToAdd.Length;
    }

    private async Task<int> AddNewUserOffers(List<JobOffer> jobs, CancellationToken cancellationToken)
    {
        var keys = jobs.Select(jo => jo.OfferUrl);

        var existingUserOffers = await DbContext.UserOffers
            .Where(j => keys.Contains(j.OfferUrl))
            .Select(j => j.OfferUrl)
            .ToArrayAsync(cancellationToken);

        var userOffersToAdd = jobs
            .Where(jo => !existingUserOffers.Contains(jo.OfferUrl))
            .Select(jo => new UserOffer(jo).ProcessKeywords(ScrapeConfig))
            .ToArray();

        Logger.LogInformation("Saving {JobsCount} new user offers", userOffersToAdd.Length);

        await DbContext.AddRangeAsync(userOffersToAdd, cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);

        return userOffersToAdd.Length;
    }
}
