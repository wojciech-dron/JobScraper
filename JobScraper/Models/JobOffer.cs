using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobScraper.Models;

public class JobOffer
{
    public string OfferUrl { get; set; }
    public string Title { get; set; }
    public string Origin { get; set; }
    public string? CompanyName { get; set; }
    public string Location { get; set; }
    public DateTimeOffset ScrapedAt { get; set; } = DateTimeOffset.Now;
    public List<string> OfferKeywords { get; set; } = [];

    public string? AgeInfo { get; set; } // Jjit only
    public string? Description { get; set; }
    public string? ApplyUrl { get; set; }
    public string? HtmlPath { get; set; }
    public string? ScreenShotPath { get; set; }
    public List<string> MyKeywords { get; set; } = [];
    public string? Salary { get; set; }

    public Company Company { get; set; } = null!;
}

public class JobOfferModelBuilder : IEntityTypeConfiguration<JobOffer>
{
    public void Configure(EntityTypeBuilder<JobOffer> builder)
    {
        builder.ToTable("JobOffers");

        builder.HasKey(j => j.OfferUrl);
        builder.Property(j => j.Title).HasMaxLength(255);
        builder.Property(j => j.Origin).HasMaxLength(24);
        builder.Property(j => j.CompanyName).HasMaxLength(255);
        builder.Property(j => j.Location).HasMaxLength(255);

        builder.Property(j => j.Description).HasMaxLength(5000);
        builder.Property(j => j.ApplyUrl).HasMaxLength(2048);
        builder.Property(j => j.HtmlPath).HasMaxLength(1024);
        builder.Property(j => j.ScreenShotPath).HasMaxLength(1024);
        builder.PrimitiveCollection(j => j.MyKeywords);

        builder.Property(j => j.Salary).HasMaxLength(128);
        builder.PrimitiveCollection(j => j.OfferKeywords);

        builder.HasIndex(j => j.ScrapedAt);
        builder.HasIndex(j => j.Location);
        builder.HasIndex(j => j.CompanyName);
        builder.HasIndex(j => j.Salary);
        builder.HasIndex(j => j.AgeInfo);
    }
}