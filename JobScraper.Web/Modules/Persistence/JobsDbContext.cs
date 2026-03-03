using JobScraper.Web.Common.Entities;
using JobScraper.Web.Modules.Auth;
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
    public DbSet<AiSummaryConfig> AiSummaryConfigs { get; set; }

    public DbSet<TimeTickerEntity> TimeTickers { get; set; }
    public DbSet<CronTickerEntity> CronTickers { get; set; }
    public DbSet<CvEntity> Cvs { get; set; }
    public DbSet<ImageEntity> CvImages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new JobOfferModelBuilder());
        modelBuilder.ApplyConfiguration(new UserJobOfferModelBuilder());
        modelBuilder.ApplyConfiguration(new CompanyModelBuilder());
        modelBuilder.ApplyConfiguration(new ApplicationModelBuilder());
        modelBuilder.ApplyConfiguration(new ScraperConfigModelBuilder());
        modelBuilder.ApplyConfiguration(new AiSummaryConfigModelBuilder());
        modelBuilder.ApplyConfiguration(new CvEntityModelBuilder());
        modelBuilder.ApplyConfiguration(new ImageEntityModelBuilder());
        modelBuilder.ApplyConfiguration(new RoleConfiguration());

        modelBuilder.ApplyConfiguration(new TimeTickerConfigurations<TimeTickerEntity>(schema: "jobs"));
        modelBuilder.ApplyConfiguration(new CronTickerConfigurations<CronTickerEntity>(schema: "jobs"));
        modelBuilder.ApplyConfiguration(new CronTickerOccurrenceConfigurations<CronTickerEntity>(schema: "jobs"));
        modelBuilder.ApplyOwnershipFilter(() => CurrentUserName);
    }
}
