using EFCore.BulkExtensions;
using JobScraper.Data;
using JobScraper.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JobScraper.Logic;

public class SyncJobOffers
{
    public record Command(List<JobOffer> Jobs) : IRequest;

    public class Handler : IRequestHandler<Command>
    {
        private readonly JobsDbContext _dbContext;
        private readonly ILogger<Handler> _logger;

        public Handler(JobsDbContext dbContext, ILogger<Handler> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }
        public async Task Handle(Command request, CancellationToken cancellationToken)
        {
            if (request.Jobs.Count == 0)
                return;

            var companies = request.Jobs
                .Select(j => j.Company)
                .Where(c => c is not null)
                .DistinctBy(c => c!.Name)
                .Cast<Company>()
                .ToList();

            await _dbContext.BulkInsertOrUpdateAsync(companies,
                cancellationToken: cancellationToken);

            foreach (var job in request.Jobs)
            {
                var companyName = job.Company?.Name;
                job.Company = null;
                job.CompanyName = companyName;
            }
            await SyncOffers(request.Jobs, cancellationToken);
        }

        private async Task SyncOffers(List<JobOffer> jobs, CancellationToken cancellationToken)
        {
            var jobOffersUrls = jobs.ConvertAll(jo => jo.OfferUrl);

            var existingJobUrls = await _dbContext.JobOffers
                .Where(jo => jobOffersUrls.Contains(jo.OfferUrl))
                .Select(jo => jo.OfferUrl)
                .ToArrayAsync(cancellationToken);

            foreach (var job in jobs)
            {
                if (existingJobUrls.Contains(job.OfferUrl))
                {
                    _dbContext.Update(job);
                }
                else
                {
                    job.FirstScrap = DateTimeOffset.Now;
                    await _dbContext.JobOffers.AddAsync(job, cancellationToken);
                }
            }

            await _dbContext.JobOffers
                .Where(jo => jo.IsActive)
                .Where(jo => !jobOffersUrls.Contains(jo.OfferUrl))
                .ForEachAsync(jo => jo.IsActive = false,
                    cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}