using JobScraper.IntegrationTests.Factories;
using JobScraper.IntegrationTests.Host;
using JobScraper.Web.Common.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobScraper.IntegrationTests.Features.Test;

public class FacetEfTests(BaseTestingFixture fixture, ITestOutputHelper outputHelper)
    : IntegrationTestBase(fixture, outputHelper)
{
    [Fact]
    public async Task JobOffer_ShouldPersistAndRetrieve_AllProperties()
    {
        // Arrange
        const string title = "Senior .NET Developer";
        const string companyName = "Acme Corp";
        const string location = "Warsaw";
        const int salaryMin = 18000;
        const int salaryMax = 25000;
        const string currency = "PLN";
        var keywords = new List<string>
        {
            "C#",
            ".NET",
            "Blazor",
        };

        var offer = ObjectMother.CreateJobOffer(
            title: title,
            companyName: companyName,
            location: location,
            salaryMinMonth: salaryMin,
            salaryMaxMonth: salaryMax,
            salaryCurrency: currency,
            offerKeywords: keywords,
            detailsScrapeStatus: DetailsScrapeStatus.Scraped);

        await ObjectMother.SaveChangesAsync();

        ResetServiceScope();

        // Act
        var result = await DbContext.JobOffers
            .Where(j => j.OfferUrl == offer.OfferUrl)
            .FirstAsync(CancellationToken);

        // Assert
        result.Title.Should().Be(title);
        result.CompanyName.Should().Be(companyName);
        result.Location.Should().Be(location);
        result.SalaryMinMonth.Should().Be(salaryMin);
        result.SalaryMaxMonth.Should().Be(salaryMax);
        result.SalaryCurrency.Should().Be(currency);
        result.OfferKeywords.Should().BeEquivalentTo(keywords);
        result.DetailsScrapeStatus.Should().Be(DetailsScrapeStatus.Scraped);
    }
}
