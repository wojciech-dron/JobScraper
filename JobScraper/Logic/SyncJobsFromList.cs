using JobScraper.Models;
using JobScraper.Persistance;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JobScraper.Logic;

public class SyncJobsFromList
{
    public record Command(List<JobOffer> Jobs) : IRequest;

    public class Handler : IRequestHandler<Command>
    {
        private readonly JobsDbContext _dbContext;
        private readonly ILogger<Handler> _logger;

        public Handler(JobsDbContext dbContext,
            ILogger<Handler> logger)
        {
            _dbContext = dbContext;
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
            var existingKeys = await _dbContext.Companies
                .Select(c => c.Name)
                .Where(key => companyNames.Contains(key))
                .ToArrayAsync(cancellationToken);

            var companiesToAdd = companyNames
                .Except(existingKeys)
                .Select(name => new Company
                {
                    Name = name
                });

            await _dbContext.Companies.AddRangeAsync(companiesToAdd, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
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

                job.ScrapedAt = DateTimeOffset.Now;
                job.AgeInfo = newScrap.AgeInfo;

            }

            var jobsToAdd = jobs
                .Where(jo => !existingJobs
                    .Select(j => j.OfferUrl)
                    .Contains(jo.OfferUrl));

            await _dbContext.JobOffers.AddRangeAsync(jobsToAdd, cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}