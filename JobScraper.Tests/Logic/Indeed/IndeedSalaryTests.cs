using JobScraper.Web.Common.Entities;
using JobScraper.Web.Features.JobOffers.Scrape.Logic.Common;
using Shouldly;
using SalaryParser = JobScraper.Web.Features.JobOffers.Scrape.Logic.Common.SalaryParser;

namespace JobScraper.Tests.Logic.Indeed;

public class IndeedSalaryTests
{
    [Theory]
    [InlineData("$50,000 - $75,000 a year", 4166, 6250, "USD")]
    [InlineData("$30 - $50 an hour", 4800, 8000, "USD")]
    [InlineData("$100,000 - $150,000 a year", 8333, 12500, "USD")]
    [InlineData("$25 - $35 an hour", 4000, 5600, "USD")]
    [InlineData("$80,000 - $120,000 a year", 6666, 10000, "USD")]
    [InlineData("$200 - $300 a day", 4000, 6000, "USD")]
    [InlineData("$1,000 - $1,500 a week", 4000, 6000, "USD")]
    [InlineData("$5,000 - $7,000 a month", 5000, 7000, "USD")]
    public void TryParseSalary_IndeedFormat_SetsCorrectValues(string rawSalary,
        int expectedMin, int expectedMax, string expectedCurrency)
    {
        // Arrange
        var jobOffer = new JobOffer();

        // Act
        var isParsed = SalaryParser.TryParseSalary(jobOffer, rawSalary);

        // Assert
        isParsed.ShouldBe(true);
        jobOffer.SalaryMinMonth.ShouldBe(expectedMin);
        jobOffer.SalaryMaxMonth.ShouldBe(expectedMax);
        jobOffer.SalaryCurrency.ShouldBe(expectedCurrency);
    }

    [Fact]
    public void TryParseSalary_EmptyIndeedSalary_ReturnsFalse()
    {
        var jobOffer = new JobOffer();

        var isParsed = SalaryParser.TryParseSalary(jobOffer, "");

        isParsed.ShouldBe(false);
        jobOffer.SalaryMinMonth.ShouldBeNull();
    }
}
