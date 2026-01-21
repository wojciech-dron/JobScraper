using JobScraper.Web.Modules.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobScraper.Web.Common.Entities;

public class ScraperConfig : IOwnable, IUpdatable
{
    public string? Owner { get; set; } = "system";
    public DateTime? UpdatedAt { get; set; }

    public float WaitForListSeconds { get; set; } = 10;
    public float WaitForScrollSeconds { get; set; } = 5;
    public float WaitForDetailsSeconds { get; set; } = 5;

    public bool SaveScreenshots { get; set; }
    public bool SavePages { get; set; }

    public bool ShowBrowserWhenScraping { get; set; }
    public BrowserTypeEnum BrowserType { get; set; } = BrowserTypeEnum.Chromium;

    public bool StarMyKeywords { get; set; }
    public List<string> MyKeywords { get; set; } = [];
    public List<string> AvoidKeywords { get; set; } = [];
    public List<SourceConfig> Sources { get; set; } = [];

    public string ScrapeCron { get; set; } = "0 15 * * *"; // default: every day at 15:00

    public DataOrigin[] GetEnabledOrigins() => Sources.Where(x => !x.Disabled).Select(x => x.DataOrigin).ToArray();
    public bool IsEnabled(DataOrigin origin) => Sources.Any(x => x.DataOrigin == origin && !x.Disabled);
}

public enum BrowserTypeEnum
{
    Chromium,
    Webkit,
    Firefox,
}

public class SourceConfig
{
    // ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
    public string SearchUrl { get; set; } = "";
    public DataOrigin DataOrigin { get; set; } = DataOrigin.PracujPl;
    public bool Disabled { get; set; } = false;
}

public class ScraperConfigModelBuilder : IEntityTypeConfiguration<ScraperConfig>
{
    public void Configure(EntityTypeBuilder<ScraperConfig> builder)
    {
        builder.ToTable("ScraperConfigs");

        builder.HasKey(x => x.Owner);
        builder.Property(j => j.Owner).HasMaxLength(255);

        builder.Property(x => x.WaitForListSeconds);
        builder.Property(x => x.WaitForScrollSeconds);
        builder.Property(x => x.WaitForDetailsSeconds);

        builder.Property(x => x.BrowserType).HasConversion<string>();
        builder.PrimitiveCollection(x => x.MyKeywords);
        builder.PrimitiveCollection(x => x.AvoidKeywords);

        builder.Property(x => x.ScrapeCron).HasMaxLength(256);

        builder.OwnsMany(x => x.Sources, b => b.ToJson("SourcesJson"));
    }
}
