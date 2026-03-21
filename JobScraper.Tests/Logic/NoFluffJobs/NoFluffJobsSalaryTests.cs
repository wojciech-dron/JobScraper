using JobScraper.Web.Common.Entities;
using JobScraper.Web.Features.JobOffers.Scrape.Logic.Common;
using Shouldly;
using SalaryParser = JobScraper.Web.Features.JobOffers.Scrape.Logic.Common.SalaryParser;

namespace JobScraper.Tests.Logic.NoFluffJobs;

public class NoFluffJobsSalaryTests
{
    [Theory]
    [InlineData("19 000 \u2013 24 000 PLN", 19000, 24000, "PLN")]
    [InlineData("18000\u201324000PLN", 18000, 24000, "PLN")]
    [InlineData("15 000 \u2013 20 000 EUR", 15000, 20000, "EUR")]
    [InlineData("10 000 \u2013 15 000 PLN", 10000, 15000, "PLN")]
    [InlineData("25000\u201335000PLN", 25000, 35000, "PLN")]
    [InlineData("8 000 \u2013 12 000 USD", 8000, 12000, "USD")]
    public void TryParseSalary_NoFluffJobsFormat_SetsCorrectValues(string rawSalary,
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
}
