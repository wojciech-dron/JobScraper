using JobScraper.Web.Modules.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobScraper.Web.Common.Entities;

public class AiSummaryConfig : IOwnable
{
    public string? Owner { get; set; } = "system";

    public bool AiSummaryEnabled { get; set; }
    public string ProviderName { get; set; }

    public string CvContent { get; set; } = "";
    public string? UserRequirements { get; set; }
    public string? TestOfferContent { get; set; }
}

public class AiSummaryConfigModelBuilder : IEntityTypeConfiguration<AiSummaryConfig>
{
    public void Configure(EntityTypeBuilder<AiSummaryConfig> builder)
    {
        builder.ToTable("AiSummaryConfigs");

        builder.HasKey(x => x.Owner);
        builder.Property(j => j.Owner).HasMaxLength(255);

        builder.Property(x => x.AiSummaryEnabled);
        builder.Property(x => x.ProviderName).HasMaxLength(100);
        builder.Property(x => x.UserRequirements).HasMaxLength(500);
        builder.Property(x => x.CvContent).HasMaxLength(10_000);
        builder.Property(x => x.TestOfferContent).HasMaxLength(10_000);
    }
}
