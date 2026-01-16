using JobScraper.Web.Modules.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobScraper.Web.Common.Entities;

public class Application : IUpdatable
{
    public string OfferUrl { get; set; } = null!;
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    public string SentCv { get; set; } = "";
    public DateTime? RespondedAt { get; set; }
    public string? Comments { get; set; }
    public int? ExpectedSalary { get; set; }
    public string? ExpectedSalaryCurrency { get; set; }
    public ApplyStatus Status { get; set; } = ApplyStatus.Applied;

    public JobOffer JobOffer { get; set; } = null!;
    public DateTime? UpdatedAt { get; set; }
}

public enum ApplyStatus
{
    Applied = 0,
    Responded = 1,
    AfterInterview = 2,
    OfferReceived = 3,
    OnHold = 4,
    FutureOffers = 5,
    Rejected = 100,
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
        builder.Property(j => j.Status).HasConversion<string>().HasMaxLength(24);
        builder.Property(j => j.ExpectedSalaryCurrency).HasMaxLength(10);


        builder.HasOne(e => e.JobOffer)
            .WithOne(e => e.Application)
            .HasForeignKey<Application>("OfferUrl")
            .HasPrincipalKey<JobOffer>(x => x.OfferUrl);

        builder.HasIndex(x => x.AppliedAt);
        builder.HasIndex(x => x.ExpectedSalary);
    }
}
