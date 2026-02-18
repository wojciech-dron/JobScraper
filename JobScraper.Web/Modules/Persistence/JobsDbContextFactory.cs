using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace JobScraper.Web.Modules.Persistence;

public class JobsDbContextFactory : IDesignTimeDbContextFactory<JobsDbContext>
{
    public JobsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<JobsDbContext>();

        var connectionString = args.FirstOrDefault() ?? "";
        optionsBuilder.UseSqlite(connectionString);

        return new JobsDbContext(optionsBuilder.Options);
    }
}
