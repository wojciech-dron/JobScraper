using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace JobScraper.Persistence;

public class JobsDbContextFactory : IDesignTimeDbContextFactory<JobsDbContext>
{
    public JobsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<JobsDbContext>();
        optionsBuilder.UseSqlite(@"Data Source=.\Data\Jobs.db");

        return new JobsDbContext(optionsBuilder.Options);
    }
}