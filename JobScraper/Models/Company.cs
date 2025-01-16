using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobScraper.Models;

public class Company
{
    public string Name { get; set; }
    public DateTimeOffset ScrapedAt { get; set; } = DateTimeOffset.Now;
    public string? IndeedUrl { get; set; }
    public string? JjitUrl { get; set; }

    public List<JobOffer> JobOffers { get; set; }
}

public class CompanyModelBuilder : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("Companies");

        builder.HasKey(b => b.Name);

        builder.HasMany(c => c.JobOffers)
            .WithOne(jo => jo.Company)
            .HasForeignKey(jo => jo.CompanyName)
            .HasPrincipalKey(c => c.Name);
    }
}
