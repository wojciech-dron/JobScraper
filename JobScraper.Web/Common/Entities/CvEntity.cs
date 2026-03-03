using JobScraper.Web.Common.Models;
using JobScraper.Web.Features.Cv.PdfGeneration;
using JobScraper.Web.Modules.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobScraper.Web.Common.Entities;

public class CvEntity : IUpdatable, IOwnable
{
    public long Id { get; set; }
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? Owner { get; set; }

    public string Name { get; set; } = "";
    public bool IsTemplate { get; set; }
    public string MarkdownContent { get; set; } = "";
    public LayoutConfig LayoutConfig { get; set; } = new();
    public string Disclaimer { get; set; } = "";
    public List<ChatItem>? ChatHistory { get; set; }

    public ImageEntity? Image { get; set; }
    public CvEntity? OriginCv { get; set; }
    public ICollection<CvEntity> DerivedCvs { get; set; } = [];
    public ICollection<UserOffer> Offers { get; set; } = [];
}

public class CvEntityModelBuilder : IEntityTypeConfiguration<CvEntity>
{
    public void Configure(EntityTypeBuilder<CvEntity> builder)
    {
        builder.ToTable("Cvs");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).HasMaxLength(255);
        builder.Property(c => c.MarkdownContent).HasMaxLength(30000);
        builder.Property(c => c.Disclaimer).HasMaxLength(1000);
        builder.Property(c => c.Owner).HasMaxLength(255);
        builder.Property(c => c.IsTemplate);

        builder.OwnsOne(c => c.LayoutConfig).ToJson();
        builder.OwnsMany(c => c.ChatHistory).ToJson();

        builder.HasOne(c => c.Image)
            .WithMany(i => i.Cvs)
            .HasForeignKey("ImageId");

        builder.HasOne(c => c.OriginCv)
            .WithMany(c => c.DerivedCvs)
            .HasForeignKey("OriginCvId")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(c => c.Offers)
            .WithOne(c => c.Cv)
            .HasForeignKey("CvId")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(c => c.CreatedAt);
        builder.HasIndex(c => c.UpdatedAt);
        builder.HasIndex(c => c.IsTemplate);
        builder.HasIndex("OriginCvId");
    }
}
