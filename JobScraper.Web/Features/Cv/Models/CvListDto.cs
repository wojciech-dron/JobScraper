using EntityFrameworkCore.Projectables;
using Facet;
using JobScraper.Web.Common.Entities;

namespace JobScraper.Web.Features.Cv.Models;

[Facet(typeof(CvEntity),
    Include =
    [
        nameof(CvEntity.Id),
        nameof(CvEntity.Name),
        nameof(CvEntity.IsTemplate),
        nameof(CvEntity.CreatedAt),
        nameof(CvEntity.UpdatedAt),

    ])]
public partial class CvListDto
{
    [MapFrom("OriginCv != null")]
    public bool HasOrigin { get; init; }

    [MapFrom("Image != null")]
    public bool HasImage { get; init; }

    [MapFrom("OriginCv!.Name")]
    public string? OriginName { get; init; }

    [MapFrom("Offers.Count")]
    public int OffersCount { get; init; }

    [MapFrom("DerivedCvs.Count")]
    public int DerivedCvsCount { get; init; }

    [MapFrom("OfferHidden()")]
    public bool OfferHidden { get; init; }
}

public static class CvEntityExtensions
{
    [Projectable]
    public static bool OfferHidden(this CvEntity cv) => cv.Offers.Any(o => o.HideStatus == HideStatus.Hidden);
}
