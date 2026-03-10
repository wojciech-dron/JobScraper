using JobScraper.IntegrationTests.Host.Services;
using JobScraper.Web.Common.Entities;

namespace JobScraper.IntegrationTests.Factories;

public static class CompanyFactory
{
    public static Company CreateCompany(this ObjectMother objectMother,
        string? name = "Test Company",
        string? description = null,
        DateTime? scrapedAt = null,
        string? indeedUrl = null,
        string? jjitUrl = null,
        string? noFluffJobsUrl = null)
    {
        var entity = new Company
        {
            Name = name!,
            Description = description,
            ScrapedAt = scrapedAt ?? DateTime.UtcNow,
            IndeedUrl = indeedUrl,
            JjitUrl = jjitUrl,
            NoFluffJobsUrl = noFluffJobsUrl,
        };

        objectMother.Add(entity);

        return entity;
    }
}
