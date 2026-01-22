using JobScraper.Web.Modules.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobScraper.Web.Common.Entities;

/// <summary> Junction entity between User and JobOffer </summary>
public record UserOffer : IOwnable, IUpdatable
{
    /// <remarks> It is unnecessary to manual set of owner. It will be handled by <see cref="OwnerInterceptor"/> </remarks>
    public string? Owner { get; set; } = "system";
    public string OfferUrl { get; set; } = null!;

    public HideStatus HideStatus { get; set; }
    public List<string> MyKeywords { get; set; } = [];
    public string Comments { get; set; } = "";

    public JobOffer Details { get; set; } = null!;
    public Application? Application { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public UserOffer()
    { }

    public UserOffer(JobOffer details)
    {
        OfferUrl = details.OfferUrl;
        Details = details;
    }
}

public enum HideStatus
{
    Regular = 0,
    Hidden = 1,
    Starred = 2,
}

public class UserJobOfferModelBuilder : IEntityTypeConfiguration<UserOffer>
{
    public void Configure(EntityTypeBuilder<UserOffer> builder)
    {
        builder.ToTable("UserOffers");

        builder.HasKey(j => new
        {
            j.Owner,
            j.OfferUrl,
        });
        builder.Property(j => j.Owner).HasMaxLength(255);
        builder.Property(j => j.OfferUrl).HasMaxLength(500);
        builder.Property(j => j.Comments).HasMaxLength(500);

        builder.PrimitiveCollection(j => j.MyKeywords);

        builder.HasIndex(j => j.HideStatus);

        builder.HasOne(e => e.Details)
            .WithMany(j => j.UserOffers)
            .HasForeignKey(x => x.OfferUrl)
            .HasPrincipalKey(x => x.OfferUrl)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
