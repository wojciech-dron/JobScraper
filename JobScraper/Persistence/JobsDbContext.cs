using JobScraper.Models;
using Microsoft.EntityFrameworkCore;

namespace JobScraper.Persistance;

public class JobsDbContext : DbContext
{
    public DbSet<JobOffer> JobOffers { get; set; }
    public DbSet<Company> Companies { get; set; }

    public JobsDbContext(DbContextOptions<JobsDbContext> options) : base(options)
    { }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new JobOfferModelBuilder());
        modelBuilder.ApplyConfiguration(new CompanyModelBuilder());
    }
}