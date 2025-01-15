using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobScraper.Models;

public class Job
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Origin { get; set; }
    public string CompanyName { get; set; }
    public string Location { get; set; }
    public DateTimeOffset ScrapedAt { get; set; } = DateTimeOffset.Now;

    public string? Description { get; set; }
    public string OfferUrl { get; set; }
    public string? ApplyUrl { get; set; }
    public List<string> MyKeywords { get; set; } = [];

    public string? Salary { get; set; }
    public List<string> OfferKeywords { get; set; } = [];
    public string? AgeInfo { get; set; }

}

public class JobModelBuilder : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.ToTable("Jobs");

        builder.Property(j => j.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Property(j => j.Title).HasMaxLength(255);
        builder.Property(j => j.Origin).HasMaxLength(24);
        builder.Property(j => j.CompanyName).HasMaxLength(255);
        builder.Property(j => j.Location).HasMaxLength(255);
        builder.Property(j => j.ScrapedAt).IsRequired();

        builder.Property(j => j.Description).HasMaxLength(5000);
        builder.Property(j => j.OfferUrl).HasMaxLength(2048);
        builder.Property(j => j.ApplyUrl).HasMaxLength(2048);
        builder.PrimitiveCollection(j => j.MyKeywords);
        builder.PrimitiveCollection(j => j.OfferKeywords);

        builder.Property(j => j.Salary).HasMaxLength(64);
        builder.Property(j => j.AgeInfo).HasMaxLength(64);

    }
}