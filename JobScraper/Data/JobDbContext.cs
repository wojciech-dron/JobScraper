using Microsoft.EntityFrameworkCore;
using JobScraper.Models;

namespace JobScraper.Data;

public class JobDbContext : DbContext
{
    public DbSet<Job> Jobs { get; set; }

    public JobDbContext(DbContextOptions<JobDbContext> options) : base(options)
    { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(@"Server=localhost;Database=JobDb;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True;");
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new JobModelBuilder());
    }
}