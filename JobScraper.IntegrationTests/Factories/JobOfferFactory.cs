using JobScraper.IntegrationTests.Host.Services;
using JobScraper.Web.Common.Entities;

namespace JobScraper.IntegrationTests.Factories;

public static class JobOfferFactory
{
    public static JobOffer CreateJobOffer(this ObjectMother objectMother,
        string offerUrl = "https://example.com/job/1",
        string title = "Software Engineer",
        DataOrigin? origin = DataOrigin.Indeed,
        string? companyName = "Test Company",
        string? location = "Remote",
        DateTime? scrapedAt = null,
        List<string>? offerKeywords = null,
        string? description = "Job description",
        int? salaryMinMonth = 15000,
        int? salaryMaxMonth = 20000,
        string? salaryCurrency = "PLN",
        DateTime? publishedAt = null,
        DetailsScrapeStatus detailsScrapeStatus = DetailsScrapeStatus.ToScrape,
        string? htmlPath = null,
        string? screenShotPath = null)
    {
        var entity = new JobOffer
        {
            OfferUrl = offerUrl,
            Title = title,
            Origin = origin,
            CompanyName = companyName,
            Location = location,
            ScrapedAt = scrapedAt         ?? DateTime.UtcNow,
            OfferKeywords = offerKeywords ?? [],
            Description = description,
            SalaryMinMonth = salaryMinMonth,
            SalaryMaxMonth = salaryMaxMonth,
            SalaryCurrency = salaryCurrency,
            PublishedAt = publishedAt,
            DetailsScrapeStatus = detailsScrapeStatus,
            HtmlPath = htmlPath,
            ScreenShotPath = screenShotPath,
        };

        var company = objectMother.DbContext.Companies.Find(companyName);
        company ??= objectMother.CreateCompany(name: companyName);
        entity.Company = company;

        objectMother.Add(entity);

        return entity;
    }
}
