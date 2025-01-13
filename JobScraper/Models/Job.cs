using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobScraper.Models;

public class Job
{
    public int Id { get; set; }
    public DateTime ScrapedAt { get; set; }
    public string SearchTerm { get; set; }
    public string? Title { get; set; }
    public string? CompanyName { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }
    public string? OfferUrl { get; set; }
    public string? ApplyUrl { get; set; }
    public string? Origin { get; set; }
    public List<string> FoundKeywords { get; set; } = [];
}

public class JobModelBuilder : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.ToTable("Jobs");

        builder.Property(j => j.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Property(j => j.Origin).HasMaxLength(255);
        builder.Property(j => j.SearchTerm).HasMaxLength(255);
        builder.Property(j => j.Title).HasMaxLength(255);
        builder.Property(j => j.CompanyName).HasMaxLength(255);
        builder.Property(j => j.Location).HasMaxLength(255);
        builder.Property(j => j.Description).HasMaxLength(5000);
        builder.PrimitiveCollection(j => j.FoundKeywords);
        builder.Property(j => j.ApplyUrl).HasMaxLength(255);
        builder.Property(j => j.OfferUrl).HasMaxLength(255);
        builder.Property(j => j.ScrapedAt).IsRequired();

    }
}