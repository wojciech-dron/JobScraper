using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobScraper.Web.Common.Entities;

public class Company
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public DateTime ScrapedAt { get; set; } = DateTime.UtcNow;
    public string? IndeedUrl { get; set; }
    public string? JjitUrl { get; set; }
    public string? NoFluffJobsUrl { get; set; }

    public List<JobOffer> JobOffers { get; set; } = null!;
}

public class CompanyModelBuilder : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("Companies");

        builder.HasKey(c => c.Name);

        builder.Property(c => c.Name).HasMaxLength(255);
        builder.Property(c => c.Description).HasMaxLength(30000);
        builder.Property(c => c.IndeedUrl).HasMaxLength(1023);
        builder.Property(c => c.JjitUrl).HasMaxLength(1023);
        builder.Property(c => c.NoFluffJobsUrl).HasMaxLength(1023);

        builder.HasMany(c => c.JobOffers)
            .WithOne(jo => jo.Company)
            .HasForeignKey(jo => jo.CompanyName)
            .HasPrincipalKey(c => c.Name);
    }
}
