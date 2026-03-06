using JobScraper.Web.Features.JobOffers.Scrape.Logic.Extensions;
using JobScraper.Web.Modules.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace JobScraper.Web.Features.JobOffers.Scrape.Logic.Functions;

public class RefreshKeywordsOnOffers
{
    public record Command : IRequest<Result>;

    public record Result(int ChangeCount = 0);

    public class Handler : IRequestHandler<Command, Result>
    {
        private readonly JobsDbContext _dbContext;
        public Handler(JobsDbContext dbContext) => _dbContext = dbContext;

        public async ValueTask<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            var config = await _dbContext.ScraperConfigs
                .AsNoTracking()
                .FirstAsync(cancellationToken);

            const int batchSize = 100;
            var skip = 0;
            var changeCounter = 0;

            while (true)
            {
                var offers = await _dbContext.UserOffers
                    .Include(u => u.Details)
                    .Skip(skip)
                    .Take(batchSize)
                    .ToListAsync(cancellationToken);

                if (offers.Count == 0)
                    break;

                foreach (var offer in offers)
                {
                    offer.ProcessKeywords(config);

                    if (_dbContext.Entry(offer).State == EntityState.Modified)
                        changeCounter++;
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                skip += batchSize;
            }

            return new Result(changeCounter);
        }
    }
}
