using JobScraper.Web.Modules.Services;
using Microsoft.EntityFrameworkCore;

namespace JobScraper.Web.Modules.Persistence;

public class ScopedJobsContextFactory(
    IDbContextFactory<JobsDbContext> pooledFactory,
    IHttpContextAccessor httpContextAccessor)
    : IDbContextFactory<JobsDbContext>
{
    public JobsDbContext CreateDbContext()
    {
        var context = pooledFactory.CreateDbContext();

        context.CurrentUserName = httpContextAccessor.HttpContext?.User.Identity?.Name;

        return context;
    }
}
