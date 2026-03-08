using JobScraper.Web.Modules.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobScraper.Web.Common.Entities;

public class AiSummaryConfig : IOwnable
{
    public string? Owner { get; set; } = "system";

    public bool AiSummaryEnabled { get; set; }
    public bool CvGenerationEnabled { get; set; }
    public string DefaultAiModel { get; set; } = null!;
    public string? SmartAiModel { get; set; }

    public string? UserRequirements { get; set; }
    public string? UserCvRules { get; set; } = "Add note: 'this is for test purposes' at the end of cv";
    public string? TestOfferContent { get; set; }

    public CvEntity? DefaultCv { get; set; }
}

public class AiSummaryConfigModelBuilder : IEntityTypeConfiguration<AiSummaryConfig>
{
    public void Configure(EntityTypeBuilder<AiSummaryConfig> builder)
    {
        builder.ToTable("AiSummaryConfigs");

        builder.HasKey(x => x.Owner);
        builder.Property(j => j.Owner).HasMaxLength(255);

        builder.Property(x => x.AiSummaryEnabled);
        builder.Property(x => x.CvGenerationEnabled);
        builder.Property(x => x.DefaultAiModel).HasMaxLength(100);
        builder.Property(x => x.SmartAiModel).HasMaxLength(100);
        builder.Property(x => x.UserRequirements).HasMaxLength(2000);
        builder.Ignore(x => x.UserCvRules);
        builder.Property(x => x.UserCvRules).HasMaxLength(2000);
        builder.Property(x => x.TestOfferContent).HasMaxLength(10_000);

        builder.HasOne(x => x.DefaultCv)
            .WithMany()
            .HasForeignKey("DefaultCvId")
            .OnDelete(DeleteBehavior.SetNull);
    }
}
