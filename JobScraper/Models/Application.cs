using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobScraper.Models;

public class Application
{
    public string OfferUrl { get; set; } = null!;
    public DateTime AppliedAt { get; set; }
    public string SentCv { get; set; } = "";
    public DateTime? RespondedAt { get; set; }
    public string Comments { get; set; } = "";
    public int? ExpectedMonthSalary { get; set; }

    public JobOffer JobOffer { get; set; } = null!;
}

public class ApplicationModelBuilder : IEntityTypeConfiguration<Application>
{
    public void Configure(EntityTypeBuilder<Application> builder)
    {
        builder.ToTable("Applications");

        builder.HasKey(j => j.OfferUrl);
        builder.Property<string>("OfferUrl").HasMaxLength(500);
        builder.Property(e => e.AppliedAt).IsRequired();
        builder.Property(e => e.SentCv).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Comments).HasMaxLength(500);

        builder.HasOne(e => e.JobOffer)
            .WithOne(e => e.Application)
            .HasForeignKey<Application>("OfferUrl")
            .HasPrincipalKey<JobOffer>(x => x.OfferUrl);

        builder.HasIndex(x => x.AppliedAt);
        builder.HasIndex(x => x.ExpectedMonthSalary);
    }
}