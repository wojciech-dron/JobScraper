using EntityFrameworkCore.Projectables;
using JobScraper.Web.Modules.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobScraper.Web.Common.Entities;

public class JobOffer : IUpdatable
{
    public string OfferUrl { get; set; } = null!;
    public DateTime? UpdatedAt { get; set; }

    public string Title { get; set; } = null!;
    public DataOrigin? Origin { get; set; }
    public string? CompanyName { get; set; }
    public string? Location { get; set; }
    public DateTime ScrapedAt { get; set; } = DateTime.UtcNow;
    public List<string> OfferKeywords { get; set; } = [];
    public string? Description { get; set; }
    public int? SalaryMinMonth { get; set; }
    public int? SalaryMaxMonth { get; set; }
    public string? SalaryCurrency { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DetailsScrapeStatus DetailsScrapeStatus { get; set; } = DetailsScrapeStatus.ToScrape;
    public string? HtmlPath { get; set; }
    public string? ScreenShotPath { get; set; }

    public Company? Company { get; set; }
    public List<UserOffer> UserOffers { get; set; } = null!;

    /// <remarks> Ownable query filter should make this work </remarks>
    [Projectable(NullConditionalRewriteSupport = NullConditionalRewriteSupport.Rewrite)]
    public UserOffer? UserOffer => UserOffers?.FirstOrDefault();
}

public enum DetailsScrapeStatus
{
    ToScrape,
    Scraped,
    Failed,
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
        builder.Property(j => j.Location).HasMaxLength(100);
        // builder.Property(j => j.Comments).HasMaxLength(500);
        builder.Property(j => j.DetailsScrapeStatus)
            .HasConversion<string>()
            .HasMaxLength(24)
            .HasDefaultValue(DetailsScrapeStatus.ToScrape);

        builder.Property(j => j.Description).HasMaxLength(5000);
        builder.Property(j => j.HtmlPath).HasMaxLength(1024);
        builder.Property(j => j.ScreenShotPath).HasMaxLength(1024);
        // builder.PrimitiveCollection(j => j.MyKeywords);

        builder.Property(j => j.SalaryCurrency).HasMaxLength(10);
        builder.PrimitiveCollection(j => j.OfferKeywords);

        // builder.HasIndex(j => j.HideStatus);
        builder.HasIndex(j => j.ScrapedAt);
        builder.HasIndex(j => j.UpdatedAt);
        builder.HasIndex(j => j.Location);
        builder.HasIndex(j => j.CompanyName);
        builder.HasIndex(j => j.SalaryMinMonth);
        builder.HasIndex(j => j.SalaryMaxMonth);
        builder.HasIndex(j => j.SalaryCurrency);
        builder.HasIndex(j => j.DetailsScrapeStatus);
    }
}
