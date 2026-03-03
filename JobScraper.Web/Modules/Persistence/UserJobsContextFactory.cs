using Microsoft.EntityFrameworkCore;

namespace JobScraper.Web.Modules.Persistence;

public class UserJobsContextFactory(
    IDbContextFactory<JobsDbContext> pooledFactory,
    IHttpContextAccessor httpContextAccessor)
    : IDbContextFactory<JobsDbContext>
{
    public JobsDbContext CreateDbContext()
    {
        var context = pooledFactory.CreateDbContext();

        // get the current username with each factory invocation
        context.CurrentUserName = httpContextAccessor.HttpContext?.User.Identity?.Name;

        return context;
    }
}
