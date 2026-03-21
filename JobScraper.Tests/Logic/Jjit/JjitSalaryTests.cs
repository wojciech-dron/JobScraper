using JobScraper.Web.Common.Entities;
using JobScraper.Web.Features.JobOffers.Scrape.Logic.Common;
using Shouldly;
using SalaryParser = JobScraper.Web.Features.JobOffers.Scrape.Logic.Common.SalaryParser;

namespace JobScraper.Tests.Logic.Jjit;

public class JjitSalaryTests
{
    [Theory]
    [InlineData("20 000 - 26 000 PLN/month", 20000, 26000, "PLN")]
    [InlineData("100 - 130 PLN/h", 16000, 20800, "PLN")]
    [InlineData("15 000 - 20 000 PLN/month", 15000, 20000, "PLN")]
    [InlineData("200 - 250 PLN/day", 4000, 5000, "PLN")]
    [InlineData("8 000 - 12 000 EUR/month", 8000, 12000, "EUR")]
    [InlineData("50 - 80 USD/h", 8000, 12800, "USD")]
    [InlineData("25 000 - 35 000 PLN/month", 25000, 35000, "PLN")]
    [InlineData("120 - 160 PLN/h", 19200, 25600, "PLN")]
    [InlineData("16 868.03 - 18 742.25 PLN", 16868, 18742, "PLN")]
    [InlineData("7 500.50 - 10 000.75 EUR/month", 7500, 10000, "EUR")]
    [InlineData("16 868.03 - 20 616.47 PLN/month", 16868, 20616, "PLN")]
    public void TryParseSalary_JjitFormat_SetsCorrectValues(string rawSalary,
        int expectedMin,
        int expectedMax,
        string expectedCurrency)
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
    public void TryParseSalary_EmptySalary_ReturnsFalse()
    {
        var jobOffer = new JobOffer();

        var isParsed = SalaryParser.TryParseSalary(jobOffer, "");

        isParsed.ShouldBe(false);
        jobOffer.SalaryMinMonth.ShouldBeNull();
    }
}
