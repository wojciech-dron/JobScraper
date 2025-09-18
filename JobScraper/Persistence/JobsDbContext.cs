using JobScraper.Models;
using JobScraper.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using TickerQ.EntityFrameworkCore.Configurations;

namespace JobScraper.Persistence;

public class JobsDbContext : DbContext
{
    public DbSet<JobOffer> JobOffers { get; set; }
    public DbSet<Company> Companies { get; set; }
    public DbSet<Application> Applications { get; set; }
    public DbSet<ScraperConfig> ScraperConfigs { get; set; }

    public JobsDbContext(DbContextOptions<JobsDbContext> options) : base(options)
    { }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new JobOfferModelBuilder());
        modelBuilder.ApplyConfiguration(new CompanyModelBuilder());
        modelBuilder.ApplyConfiguration(new ApplicationModelBuilder());
        modelBuilder.ApplyConfiguration(new ScraperConfigModelBuilder());

        modelBuilder.ApplyConfiguration(new TimeTickerConfigurations(schema: "jobs"));
        modelBuilder.ApplyConfiguration(new CronTickerConfigurations(schema: "jobs"));
        modelBuilder.ApplyConfiguration(new CronTickerOccurrenceConfigurations(schema: "jobs"));

    }
}