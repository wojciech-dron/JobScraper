using JobScraper.Web.Common.Entities;
using JobScraper.Web.Features.JobOffers.Scrape.Logic.Common;
using Shouldly;
using SalaryParser = JobScraper.Web.Features.JobOffers.Scrape.Logic.Common.SalaryParser;

namespace JobScraper.Tests.Logic.RocketJobs;

public class RocketJobsSalaryTests
{
    [Theory]
    [InlineData("20 000 - 26 000 PLN/month", 20000, 26000, "PLN")]
    [InlineData("100 - 130 PLN/h", 16000, 20800, "PLN")]
    [InlineData("15 000 - 18 000 PLN/month", 15000, 18000, "PLN")]
    [InlineData("80 - 120 PLN/h", 12800, 19200, "PLN")]
    [InlineData("10 000 - 14 000 EUR/month", 10000, 14000, "EUR")]
    public void TryParseSalary_RocketJobsFormat_SetsCorrectValues(string rawSalary,
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
