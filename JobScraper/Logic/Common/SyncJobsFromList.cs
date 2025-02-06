using JobScraper.Models;
using JobScraper.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JobScraper.Logic.Common;

public class SyncJobsFromList
{
    public record Command(List<JobOffer> Jobs) : IRequest;

    public class Handler : IRequestHandler<Command>
    {
        private readonly JobsDbContext _dbContext;
        private readonly ILogger<Handler> _logger;

        public Handler(IDbContextFactory<JobsDbContext> dbContextFactory,
            ILogger<Handler> logger)
        {
            _dbContext = dbContextFactory.CreateDbContext();
            _logger = logger;
        }
        public async Task Handle(Command request, CancellationToken cancellationToken)
        {
            if (request.Jobs.Count == 0)
                return;

            var companies = request.Jobs
                .Select(j => j.CompanyName)
                .Where(c => c is not null)
                .Distinct()
                .Cast<string>()
                .ToArray();

            await AddNewCompanies(companies, cancellationToken);
            await SyncJobs(request.Jobs, cancellationToken);
        }

        private async Task AddNewCompanies(string[] companyNames, CancellationToken cancellationToken)
        {
            // to avoid race condition
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var existingKeys = await _dbContext.Companies
                    .Where(c => companyNames.Contains(c.Name))
                    .Select(c => c.Name)
                    .ToArrayAsync(cancellationToken);

                var companiesToAdd = companyNames
                    .Except(existingKeys)
                    .Select(name => new Company
                    {
                        Name = name
                    }).ToArray();

                _logger.LogInformation("Saving {CompaniesCount} new companies", companiesToAdd.Length);

                await _dbContext.Companies.AddRangeAsync(companiesToAdd, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

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

            var existingJobs = await _dbContext.JobOffers
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

            _logger.LogInformation("Saving {JobsCount} new jobs", jobsToAdd.Length);

            await _dbContext.JobOffers.AddRangeAsync(jobsToAdd, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}