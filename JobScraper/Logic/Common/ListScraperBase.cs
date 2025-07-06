using JobScraper.Models;
using JobScraper.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JobScraper.Logic.Common;

public abstract class ListScraperBase<TScrapeCommand> : ScrapperBase, IRequestHandler<TScrapeCommand, ScrapeResponse>
    where TScrapeCommand : ScrapeCommand
{
    public ListScraperBase(IOptions<ScraperConfig> config,
        ILogger<ListScraperBase<TScrapeCommand>> logger,
        JobsDbContext dbContext) : base(config, logger, dbContext)
    { }

    public abstract IAsyncEnumerable<List<JobOffer>> ScrapeJobs(SourceConfig sourceConfig);

    public async Task<ScrapeResponse> Handle(TScrapeCommand scrape, CancellationToken cancellationToken = default)
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
        return await SyncJobs(jobs, cancellationToken);
    }

    private async Task AddNewCompanies(string[] companyNames, CancellationToken cancellationToken)
    {
        // to avoid race condition
        await using var transaction = await DbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var existingKeys = await DbContext.Companies
                .Where(c => companyNames.Contains(c.Name))
                .Select(c => c.Name)
                .ToArrayAsync(cancellationToken);

            var companiesToAdd = companyNames
                .Except(existingKeys)
                .Select(name => new Company
                {
                    Name = name
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

    private async Task<int> SyncJobs(List<JobOffer> jobs, CancellationToken cancellationToken)
    {
        var keys = jobs.ConvertAll(jo => jo.OfferUrl);

        var existingJobs = await DbContext.JobOffers
            .Where(j => keys.Contains(j.OfferUrl))
            .ToListAsync(cancellationToken);

        foreach (var job in existingJobs)
        {
            var newScrap = jobs.First(e => e.OfferUrl == job.OfferUrl);

            job.AgeInfo = newScrap.AgeInfo;
        }

        var jobsToAdd = jobs
            .Where(jo => !existingJobs
                .Select(j => j.OfferUrl)
                .Contains(jo.OfferUrl))
            .ToArray();

        Logger.LogInformation("Saving {JobsCount} new jobs", jobsToAdd.Length);

        await DbContext.JobOffers.AddRangeAsync(jobsToAdd, cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);
        return jobsToAdd.Length;
    }
}