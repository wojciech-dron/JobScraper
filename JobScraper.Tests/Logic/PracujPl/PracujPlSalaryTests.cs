using JobScraper.Web.Common.Entities;
using Shouldly;
using SalaryParser = JobScraper.Web.Features.Scrape.Logic.Common.SalaryParser;

namespace JobScraper.Tests.Logic.PracujPl;

public class PracujPlSalaryTests
{
    [Fact]
    public void TryParseSalary_FixedNettoMonthly_SetsCorrectValues()
    {
        // Arrange
        var jobOffer = new JobOffer();
        var rawSalary = "22 000 zł netto (+ VAT) / mies.";
        // Act
        SalaryParser.TryParseSalary(jobOffer, rawSalary);
        // Assert
        jobOffer.SalaryMinMonth.ShouldBe(22000);
        jobOffer.SalaryMaxMonth.ShouldBe(22000);
        jobOffer.SalaryCurrency.ShouldBe("zł");
    }

    [Fact]
    public void TryParseSalary_RangeNettoMonthly_SetsCorrectValues()
    {
        // Arrange
        var jobOffer = new JobOffer();
        var rawSalary = "11 000–16 000 zł netto (+ VAT) / mies.";
        // Act
        SalaryParser.TryParseSalary(jobOffer, rawSalary);
        // Assert
        jobOffer.SalaryMinMonth.ShouldBe(11000);
        jobOffer.SalaryMaxMonth.ShouldBe(16000);
        jobOffer.SalaryCurrency.ShouldBe("zł");
    }

    [Fact]
    public void TryParseSalary_RangeContractDependentMonthly_SetsCorrectValues()
    {
        // Arrange
        var jobOffer = new JobOffer();
        var rawSalary = "7 000–10 000 zł / mies. (zal. od umowy)";
        // Act
        SalaryParser.TryParseSalary(jobOffer, rawSalary);
        // Assert
        jobOffer.SalaryMinMonth.ShouldBe(7000);
        jobOffer.SalaryMaxMonth.ShouldBe(10000);
        jobOffer.SalaryCurrency.ShouldBe("zł");
    }

    [Fact]
    public void TryParseSalary_RangeNettoHourly_SetsCorrectValues()
    {
        // Arrange
        var jobOffer = new JobOffer();
        var rawSalary = "130–150 zł netto (+ VAT) / godz.";
        // Act
        SalaryParser.TryParseSalary(jobOffer, rawSalary);
        // Assert
        jobOffer.SalaryMinMonth.ShouldBe(20800);
        jobOffer.SalaryMaxMonth.ShouldBe(24000);
        jobOffer.SalaryCurrency.ShouldBe("zł");
    }

    [Fact]
    public void TryParseSalary_FixedBruttoMonthly_SetsCorrectValues()
    {
        // Arrange
        var jobOffer = new JobOffer();
        var rawSalary = "11 000 zł brutto / mies.";
        // Act
        SalaryParser.TryParseSalary(jobOffer, rawSalary);
        // Assert
        jobOffer.SalaryMinMonth.ShouldBe(8943);
        jobOffer.SalaryMaxMonth.ShouldBe(8943);
        jobOffer.SalaryCurrency.ShouldBe("zł");
    }

    [Fact]
    public void TryParseSalary_RangeBruttoMonthly_SetsCorrectValues()
    {
        // Arrange
        var jobOffer = new JobOffer();
        var rawSalary = "16 000–30 000 zł brutto / mies.";
        // Act
        SalaryParser.TryParseSalary(jobOffer, rawSalary);
        // Assert
        jobOffer.SalaryMinMonth.ShouldBe(13008);
        jobOffer.SalaryMaxMonth.ShouldBe(24390);
        jobOffer.SalaryCurrency.ShouldBe("zł");
    }

    [Theory]
    [InlineData("22 000 zł netto (+ VAT) / mies.", 22000, 22000, "zł")]
    [InlineData("11 000–16 000 zł netto (+ VAT) / mies.", 11000, 16000, "zł")]
    [InlineData("20 000–26 000 zł netto (+ VAT) / mies.", 20000, 26000, "zł")]
    [InlineData("7 000–10 000 zł / mies. (zal. od umowy)", 7000, 10000, "zł")]
    [InlineData("130–150 zł netto (+ VAT) / godz.", 20800, 24000, "zł")]
    [InlineData("90–120 zł netto (+ VAT) / godz.", 14400, 19200, "zł")]
    [InlineData("16 000–21 000 zł netto (+ VAT) / mies.", 16000, 21000, "zł")]
    [InlineData("100–156 zł netto (+ VAT) / godz.", 16000, 24960, "zł")]
    [InlineData("80–150 zł netto (+ VAT) / godz.", 12800, 24000, "zł")]
    [InlineData("18 000–22 000 zł netto (+ VAT) / mies.", 18000, 22000, "zł")]
    [InlineData("23 000–26 000 zł netto (+ VAT) / mies.", 23000, 26000, "zł")]
    [InlineData("15 000–26 880 zł / mies. (zal. od umowy)", 15000, 26880, "zł")]
    [InlineData("8 000–16 000 zł / mies. (zal. od umowy)", 8000, 16000, "zł")]
    [InlineData("130 zł netto (+ VAT) / mies.", 130, 130, "zł")]
    [InlineData("18 000–28 000 zł / mies. (zal. od umowy)", 18000, 28000, "zł")]
    [InlineData("12 200–25 200 zł / mies. (zal. od umowy)", 12200, 25200, "zł")]
    [InlineData("170–210 zł netto (+ VAT) / godz.", 27200, 33600, "zł")]
    [InlineData("18 000–25 000 zł netto (+ VAT) / mies.", 18000, 25000, "zł")]
    [InlineData("18 480–26 880 zł netto (+ VAT) / mies.", 18480, 26880, "zł")]
    [InlineData("11 000–14 000 zł brutto / mies.", 8943, 11382, "zł")]
    [InlineData("13 000–18 000 zł / mies. (zal. od umowy)", 13000, 18000, "zł")]
    [InlineData("16 000–30 000 zł brutto / mies.", 13008, 24390, "zł")]
    [InlineData("110–140 zł netto (+ VAT) / godz.", 17600, 22400, "zł")]
    [InlineData("120–140 zł netto (+ VAT) / godz.", 19200, 22400, "zł")]
    [InlineData("100–146 zł netto (+ VAT) / godz.", 16000, 23360, "zł")]
    [InlineData("15 600–20 000 zł netto (+ VAT) / mies.", 15600, 20000, "zł")]
    [InlineData("24 000–28 000 zł / mies. (zal. od umowy)", 24000, 28000, "zł")]
    [InlineData("18 500–28 000 zł netto (+ VAT) / mies.", 18500, 28000, "zł")]
    [InlineData("15 000–25 000 zł / mies. (zal. od umowy)", 15000, 25000, "zł")]
    [InlineData("25 000–28 000 zł netto (+ VAT) / mies.", 25000, 28000, "zł")]
    [InlineData("11 000–21 840 zł / mies. (zal. od umowy)", 11000, 21840, "zł")]
    [InlineData("10 000–17 000 zł / mies. (zal. od umowy)", 10000, 17000, "zł")]
    [InlineData("18 000–27 000 zł / mies. (zal. od umowy)", 18000, 27000, "zł")]
    [InlineData("5 000–30 000 zł / mies. (zal. od umowy)", 5000, 30000, "zł")]
    [InlineData("8 000–18 000 zł / mies. (zal. od umowy)", 8000, 18000, "zł")]
    [InlineData("15\u00a0000–26\u00a0880\u00a0zł\u00a0/ mies. (zal. od umowy)", 15000, 26880, "zł")]
    public void TryParseSalary_AllExamples_SetsCorrectValues(string rawSalary,
        int expectedMin,
        int expectedMax,
        string expectedCurrency)
    {
        // Arrange
        var jobOffer = new JobOffer();
        // Act
        SalaryParser.TryParseSalary(jobOffer, rawSalary);
        // Assert
        jobOffer.SalaryMinMonth.ShouldBe(expectedMin);
        jobOffer.SalaryMaxMonth.ShouldBe(expectedMax);
        jobOffer.SalaryCurrency.ShouldBe(expectedCurrency);
    }

    [Fact]
    public void TryParseSalary_EmptySalary_DoesNotSetValues()
    {
        // Arrange
        var jobOffer = new JobOffer();
        var rawSalary = "";
        // Act
        SalaryParser.TryParseSalary(jobOffer, rawSalary);
        // Assert
        jobOffer.SalaryMinMonth.ShouldBeNull();
        jobOffer.SalaryMaxMonth.ShouldBeNull();
        jobOffer.SalaryCurrency.ShouldBeNull();
    }

    [Fact]
    public void TryParseSalary_InvalidFormat_DoesNotSetValues()
    {
        // Arrange
        var jobOffer = new JobOffer();
        var rawSalary = "Invalid salary format";
        // Act
        SalaryParser.TryParseSalary(jobOffer, rawSalary);
        // Assert
        jobOffer.SalaryMinMonth.ShouldBeNull();
        jobOffer.SalaryMaxMonth.ShouldBeNull();
        jobOffer.SalaryCurrency.ShouldBeNull();
    }
}
