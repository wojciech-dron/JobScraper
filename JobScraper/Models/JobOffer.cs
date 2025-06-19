using JobScraper.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobScraper.Models;

public class JobOffer : IUpdatable
{
    public string OfferUrl { get; set; } = null!;
    public string Title { get; set; } = null!;
    public DataOrigin? Origin { get; set; }
    public string? CompanyName { get; set; }
    public string? Location { get; set; }
    public DateTime ScrapedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool Hidden { get; set; }
    public List<string> OfferKeywords { get; set; } = [];

    public string? Description { get; set; }
    public string? ApplyUrl { get; set; }
    public string? HtmlPath { get; set; }
    public string? ScreenShotPath { get; set; }
    public List<string> MyKeywords { get; set; } = [];
    public int? SalaryMinMonth { get; set; }
    public int? SalaryMaxMonth { get; set; }
    public string? SalaryCurrency { get; set; }
    public string Comments { get; set; } = "";

    // Jjit, rocketjobs, manual only
    public string? AgeInfo { get; set; }
    public DateTime? PublishedAt { get; set; }

    public DetailsScrapeStatus DetailsScrapeStatus { get; set; } = DetailsScrapeStatus.ToScrape;

    public Company Company { get; set; } = null!;
    public Application? Application { get; set; }
}

public enum DetailsScrapeStatus
{
    ToScrape,
    Scraped,
    Failed,
}

public enum DataOrigin
{
    Manual,
    Indeed,
    JustJoinIt,
    NoFluffJobs,
    LinkedIn,
    PracujPl,
    RocketJobs,
    Olx,
}

public class JobOfferModelBuilder : IEntityTypeConfiguration<JobOffer>
{
    public void Configure(EntityTypeBuilder<JobOffer> builder)
    {
        builder.ToTable("JobOffers");

        builder.HasKey(j => j.OfferUrl);
        builder.Property(j => j.OfferUrl).HasMaxLength(500);
        builder.Property(j => j.Title).HasMaxLength(255);
        builder.Property(j => j.Origin).HasConversion<string>().HasMaxLength(24);
        builder.Property(j => j.CompanyName).HasMaxLength(255);
        builder.Property(j => j.Location).HasMaxLength(255);
        builder.Property(j => j.AgeInfo).HasMaxLength(32);
        builder.Property(j => j.Location).HasMaxLength(100);
        builder.Property(j => j.Comments).HasMaxLength(500);
        builder.Property(j => j.DetailsScrapeStatus)
            .HasConversion<string>()
            .HasMaxLength(24)
            .HasDefaultValue(DetailsScrapeStatus.ToScrape);

        builder.Property(j => j.Description).HasMaxLength(5000);
        builder.Property(j => j.ApplyUrl).HasMaxLength(2048);
        builder.Property(j => j.HtmlPath).HasMaxLength(1024);
        builder.Property(j => j.ScreenShotPath).HasMaxLength(1024);
        builder.PrimitiveCollection(j => j.MyKeywords);

        builder.Property(j => j.SalaryCurrency).HasMaxLength(10);
        builder.PrimitiveCollection(j => j.OfferKeywords);

        builder.HasIndex(j => j.Hidden);
        builder.HasIndex(j => j.ScrapedAt);
        builder.HasIndex(j => j.UpdatedAt);
        builder.HasIndex(j => j.Location);
        builder.HasIndex(j => j.CompanyName);
        builder.HasIndex(j => j.SalaryMinMonth);
        builder.HasIndex(j => j.SalaryMaxMonth);
        builder.HasIndex(j => j.SalaryCurrency);
        builder.HasIndex(j => j.AgeInfo);
        builder.HasIndex(j => j.DetailsScrapeStatus);
    }
}