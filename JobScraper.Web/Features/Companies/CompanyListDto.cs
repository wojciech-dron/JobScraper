using Facet;
using JobScraper.Entities;

namespace JobScraper.Web.Features.Companies;

[Facet(typeof(Company), nameof(Company.JobOffers), nameof(Company.IndeedUrl))]
public partial class CompanyListDto
{
    [MapFrom(nameof(Company.Name))]
    public string Name { get; set; } = "";

    [MapFrom("JobOffers.Count")]
    public int JobOffersCount { get; init; }
}
