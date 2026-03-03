using JobScraper.Web.Modules.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobScraper.Web.Common.Entities;

public class ImageEntity : IUpdatable, IOwnable
{
    public long Id { get; set; }
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? Owner { get; set; }

    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long Size { get; set; }
    public byte[] Data { get; set; } = [];
    public ICollection<CvEntity> Cvs { get; set; } = [];
}

public class ImageEntityModelBuilder : IEntityTypeConfiguration<ImageEntity>
{
    public void Configure(EntityTypeBuilder<ImageEntity> builder)
    {
        builder.ToTable("CvImages");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.FileName).HasMaxLength(255);
        builder.Property(x => x.ContentType).HasMaxLength(100);
        builder.Property(x => x.Owner).HasMaxLength(255);
        builder.Property(x => x.Data).HasMaxLength(1024 * 1024 * 5);

        builder.HasIndex(x => x.Owner);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.UpdatedAt);
    }
}
