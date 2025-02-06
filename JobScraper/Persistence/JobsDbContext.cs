using JobScraper.Models;
using JobScraper.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace JobScraper.Persistence;

public class JobsDbContext : DbContext
{
    public DbSet<JobOffer> JobOffers { get; set; }
    public DbSet<Company> Companies { get; set; }
    public DbSet<Application> Applications { get; set; }

    public JobsDbContext(DbContextOptions<JobsDbContext> options) : base(options)
    { }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new JobOfferModelBuilder());
        modelBuilder.ApplyConfiguration(new CompanyModelBuilder());
        modelBuilder.ApplyConfiguration(new ApplicationModelBuilder());
    }
}