using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobScraper.Models;

public class Job
{
    public int Id { get; init; }
    public string Title { get; init; }
    public string Origin { get; init; }
    public string CompanyName { get; init; }
    public string Location { get; init; }
    public string OfferUrl { get; init; }
    public string SearchTerm { get; init; }
    public DateTimeOffset ScrapedAt { get; init; } = DateTimeOffset.Now;

    public string? Description { get; set; }
    public string? ApplyUrl { get; set; }
    public List<string> FoundKeywords { get; set; } = [];
}

public class JobModelBuilder : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.ToTable("Jobs");

        builder.Property(j => j.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Property(j => j.Origin).HasMaxLength(24);
        builder.Property(j => j.SearchTerm).HasMaxLength(24);
        builder.Property(j => j.Title).HasMaxLength(255);
        builder.Property(j => j.CompanyName).HasMaxLength(255);
        builder.Property(j => j.Location).HasMaxLength(255);
        builder.Property(j => j.Description).HasMaxLength(5000);
        builder.PrimitiveCollection(j => j.FoundKeywords);
        builder.Property(j => j.ApplyUrl).HasMaxLength(2048);
        builder.Property(j => j.OfferUrl).HasMaxLength(2048);
        builder.Property(j => j.ScrapedAt).IsRequired();

    }
}