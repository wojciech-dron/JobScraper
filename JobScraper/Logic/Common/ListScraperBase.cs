using JobScraper.Models;
using JobScraper.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JobScraper.Logic.Common;

public abstract class ListScraperBase<TScrapeCommand> : ScrapperBase, IRequestHandler<TScrapeCommand>
    where TScrapeCommand : ScrapeCommand
{
    protected readonly JobsDbContext DbContext;

    public ListScraperBase(IOptions<ScraperConfig> config,
        ILogger<ListScraperBase<TScrapeCommand>> logger,
        JobsDbContext dbContext) : base(config, logger)
    {
        DbContext = dbContext;
    }

    public abstract IAsyncEnumerable<List<JobOffer>> ScrapeJobs();

    public async Task Handle(TScrapeCommand scrape, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            Logger.LogWarning("Scraper is disabled. Please configure {DataOrigin} origin in scraper configuration.",
                DataOrigin);
            return;
        }

        Logger.LogInformation("Scraping {DataOrigin} jobs list...", DataOrigin);

        await foreach (var jobs in ScrapeJobs().WithCancellation(cancellationToken))
        {
            Logger.LogInformation("Syncing {DataOrigin} jobs...", DataOrigin);
            await SyncJobsFromList(jobs, cancellationToken);
        }

        Dispose();
    }

    public async Task SyncJobsFromList(List<JobOffer> jobs, CancellationToken cancellationToken)
    {
        if (jobs.Count == 0)
            return;

        var companies = jobs
            .Select(j => j.CompanyName)
            .Where(c => c is not null)
            .Distinct()
            .Cast<string>()
            .ToArray();

        await AddNewCompanies(companies, cancellationToken);
        await SyncJobs(jobs, cancellationToken);
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

    private async Task SyncJobs(List<JobOffer> jobs, CancellationToken cancellationToken)
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
    }
}