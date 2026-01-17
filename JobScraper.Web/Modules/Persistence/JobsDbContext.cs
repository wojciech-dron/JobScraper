using JobScraper.Web.Common.Entities;
using JobScraper.Web.Modules.Persistence.Interceptors;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TickerQ.EntityFrameworkCore.Configurations;
using TickerQ.Utilities.Entities;

namespace JobScraper.Web.Modules.Persistence;

public class JobsDbContext(DbContextOptions<JobsDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public string? CurrentUserName { get; set; }

    public DbSet<JobOffer> JobOffers { get; set; }
    public DbSet<UserOffer> UserOffers { get; set; }
    public DbSet<Company> Companies { get; set; }
    public DbSet<Application> Applications { get; set; }
    public DbSet<ScraperConfig> ScraperConfigs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new JobOfferModelBuilder());
        modelBuilder.ApplyConfiguration(new UserJobOfferModelBuilder());
        modelBuilder.ApplyConfiguration(new CompanyModelBuilder());
        modelBuilder.ApplyConfiguration(new ApplicationModelBuilder());
        modelBuilder.ApplyConfiguration(new ScraperConfigModelBuilder());

        modelBuilder.ApplyConfiguration(new TimeTickerConfigurations<TimeTickerEntity>(schema: "jobs"));
        modelBuilder.ApplyConfiguration(new CronTickerConfigurations<CronTickerEntity>(schema: "jobs"));
        modelBuilder.ApplyConfiguration(new CronTickerOccurrenceConfigurations<CronTickerEntity>(schema: "jobs"));
        modelBuilder.ApplyOwnershipFilter(() => CurrentUserName);
    }
}
