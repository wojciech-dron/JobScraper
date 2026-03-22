using JobScraper.Web.Modules.Persistence.Interceptors;
using JobScraper.Web.Modules.Persistence.Seed.CustomScrapers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobScraper.Web.Common.Entities;

public class CustomScraperConfig : IUpdatable
{
    public long Id { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public string DataOrigin { get; set; } = "";
    public string ListScraperScript { get; set; } = "";
    public bool DetailsScrapingEnabled { get; set; }
    public string? DetailsScraperScript { get; set; }
    public string? PaginationScript { get; set; }
    public string Domain { get; set; } = "";
    public string? TestListUrl { get; set; }
    public string? TestDetailsUrl { get; set; }
}

public class CustomScraperConfigModelBuilder : IEntityTypeConfiguration<CustomScraperConfig>
{
    public void Configure(EntityTypeBuilder<CustomScraperConfig> builder)
    {
        builder.ToTable("CustomScraperConfigs");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.DataOrigin).HasMaxLength(100);
        builder.Property(x => x.ListScraperScript).HasMaxLength(10000);
        builder.Property(x => x.DetailsScraperScript).HasMaxLength(10000);
        builder.Property(x => x.PaginationScript).HasMaxLength(10000);
        builder.Property(x => x.Domain).HasMaxLength(253);
        builder.Property(x => x.TestListUrl).HasMaxLength(2048);
        builder.Property(x => x.TestDetailsUrl).HasMaxLength(2048);

        builder.HasData(CustomScraperSeeder.GetData());
    }
}
