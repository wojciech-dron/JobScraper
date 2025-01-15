using Microsoft.EntityFrameworkCore;
using JobScraper.Models;

namespace JobScraper.Data;

public class JobsDbContext : DbContext
{
    public DbSet<Job> Jobs { get; set; }

    public JobsDbContext(DbContextOptions<JobsDbContext> options) : base(options)
    { }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new JobModelBuilder());
    }
}