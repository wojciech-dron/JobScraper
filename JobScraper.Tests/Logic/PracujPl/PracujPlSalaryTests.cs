using JobScraper.Logic.PracujPl;
using JobScraper.Models;

namespace JobScraper.Tests.Logic.PracujPl;

public class PracujPlSalaryTests
{

    [Fact]
    public void SetSalary_FixedNettoMonthly_SetsCorrectValues()
    {
        // Arrange
        var jobOffer = new JobOffer();
        var rawSalary = "22 000 zł netto (+ VAT) / mies.";

        // Act
        SalaryParser.SetSalary(jobOffer, rawSalary);

        // Assert
        Assert.Equal(22000, jobOffer.SalaryMinMonth);
        Assert.Equal(22000, jobOffer.SalaryMaxMonth);
        Assert.Equal("zł", jobOffer.SalaryCurrency);
    }

    [Fact]
    public void SetSalary_RangeNettoMonthly_SetsCorrectValues()
    {
        // Arrange
        var jobOffer = new JobOffer();
        var rawSalary = "11 000–16 000 zł netto (+ VAT) / mies.";

        // Act
        SalaryParser.SetSalary(jobOffer, rawSalary);

        // Assert
        Assert.Equal(11000, jobOffer.SalaryMinMonth);
        Assert.Equal(16000, jobOffer.SalaryMaxMonth);
        Assert.Equal("zł", jobOffer.SalaryCurrency);
    }

    [Fact]
    public void SetSalary_RangeContractDependentMonthly_SetsCorrectValues()
    {
        // Arrange
        var jobOffer = new JobOffer();
        var rawSalary = "7 000–10 000 zł / mies. (zal. od umowy)";

        // Act
        SalaryParser.SetSalary(jobOffer, rawSalary);

        // Assert
        Assert.Equal(7000, jobOffer.SalaryMinMonth);
        Assert.Equal(10000, jobOffer.SalaryMaxMonth);
        Assert.Equal("zł", jobOffer.SalaryCurrency);
    }

    [Fact]
    public void SetSalary_RangeNettoHourly_SetsCorrectValues()
    {
        // Arrange
        var jobOffer = new JobOffer();
        var rawSalary = "130–150 zł netto (+ VAT) / godz.";

        // Act
        SalaryParser.SetSalary(jobOffer, rawSalary);

        // Assert
        // Hourly rate converted to monthly (130 * 12 * 30 * 24 / 12 = 93600)
        Assert.Equal(93600, jobOffer.SalaryMinMonth);
        Assert.Equal(108000, jobOffer.SalaryMaxMonth);
        Assert.Equal("zł", jobOffer.SalaryCurrency);
    }

    [Fact]
    public void SetSalary_FixedBruttoMonthly_SetsCorrectValues()
    {
        // Arrange
        var jobOffer = new JobOffer();
        var rawSalary = "11 000 zł brutto / mies.";

        // Act
        SalaryParser.SetSalary(jobOffer, rawSalary);

        // Assert
        // Brutto to netto conversion: 11000 / 1.23 ≈ 8943
        Assert.Equal(8943, jobOffer.SalaryMinMonth);
        Assert.Equal(8943, jobOffer.SalaryMaxMonth);
        Assert.Equal("zł", jobOffer.SalaryCurrency);
    }

    [Fact]
    public void SetSalary_RangeBruttoMonthly_SetsCorrectValues()
    {
        // Arrange
        var jobOffer = new JobOffer();
        var rawSalary = "16 000–30 000 zł brutto / mies.";

        // Act
        SalaryParser.SetSalary(jobOffer, rawSalary);

        // Assert
        // Brutto to netto conversion: 16000 / 1.23 ≈ 13008, 30000 / 1.23 ≈ 24390
        Assert.Equal(13008, jobOffer.SalaryMinMonth);
        Assert.Equal(24390, jobOffer.SalaryMaxMonth);
        Assert.Equal("zł", jobOffer.SalaryCurrency);
    }

    [Theory]
    [InlineData("22 000 zł netto (+ VAT) / mies.", 22000, 22000, "zł")]
    [InlineData("11 000–16 000 zł netto (+ VAT) / mies.", 11000, 16000, "zł")]
    [InlineData("20 000–26 000 zł netto (+ VAT) / mies.", 20000, 26000, "zł")]
    [InlineData("7 000–10 000 zł / mies. (zal. od umowy)", 7000, 10000, "zł")]
    [InlineData("130–150 zł netto (+ VAT) / godz.", 93600, 108000, "zł")]
    [InlineData("90–120 zł netto (+ VAT) / godz.", 64800, 86400, "zł")]
    [InlineData("16 000–21 000 zł netto (+ VAT) / mies.", 16000, 21000, "zł")]
    [InlineData("100–156 zł netto (+ VAT) / godz.", 72000, 112320, "zł")]
    [InlineData("80–150 zł netto (+ VAT) / godz.", 57600, 108000, "zł")]
    [InlineData("18 000–22 000 zł netto (+ VAT) / mies.", 18000, 22000, "zł")]
    [InlineData("23 000–26 000 zł netto (+ VAT) / mies.", 23000, 26000, "zł")]
    [InlineData("15 000–26 880 zł / mies. (zal. od umowy)", 15000, 26880, "zł")]
    [InlineData("8 000–16 000 zł / mies. (zal. od umowy)", 8000, 16000, "zł")]
    [InlineData("130 zł netto (+ VAT) / mies.", 130, 130, "zł")]
    [InlineData("18 000–28 000 zł / mies. (zal. od umowy)", 18000, 28000, "zł")]
    [InlineData("12 200–25 200 zł / mies. (zal. od umowy)", 12200, 25200, "zł")]
    [InlineData("170–210 zł netto (+ VAT) / godz.", 122400, 151200, "zł")]
    [InlineData("18 000–25 000 zł netto (+ VAT) / mies.", 18000, 25000, "zł")]
    [InlineData("18 480–26 880 zł netto (+ VAT) / mies.", 18480, 26880, "zł")]
    [InlineData("11 000–14 000 zł brutto / mies.", 8943, 11382, "zł")]
    [InlineData("13 000–18 000 zł / mies. (zal. od umowy)", 13000, 18000, "zł")]
    [InlineData("16 000–30 000 zł brutto / mies.", 13008, 24390, "zł")]
    [InlineData("110–140 zł netto (+ VAT) / godz.", 79200, 100800, "zł")]
    [InlineData("120–140 zł netto (+ VAT) / godz.", 86400, 100800, "zł")]
    [InlineData("100–146 zł netto (+ VAT) / godz.", 72000, 105120, "zł")]
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
    public void SetSalary_AllExamples_SetsCorrectValues(string rawSalary, int expectedMin, int expectedMax, string expectedCurrency)
    {
        // Arrange
        var jobOffer = new JobOffer();

        // Act
        SalaryParser.SetSalary(jobOffer, rawSalary);

        // Assert
        Assert.Equal(expectedMin, jobOffer.SalaryMinMonth);
        Assert.Equal(expectedMax, jobOffer.SalaryMaxMonth);
        Assert.Equal(expectedCurrency, jobOffer.SalaryCurrency);
    }

    [Fact]
    public void SetSalary_EmptySalary_DoesNotSetValues()
    {
        // Arrange
        var jobOffer = new JobOffer();
        var rawSalary = "";

        // Act
        SalaryParser.SetSalary(jobOffer, rawSalary);

        // Assert
        Assert.Null(jobOffer.SalaryMinMonth);
        Assert.Null(jobOffer.SalaryMaxMonth);
        Assert.Null(jobOffer.SalaryCurrency);
    }

    [Fact]
    public void SetSalary_InvalidFormat_DoesNotSetValues()
    {
        // Arrange
        var jobOffer = new JobOffer();
        var rawSalary = "Invalid salary format";

        // Act
        SalaryParser.SetSalary(jobOffer, rawSalary);

        // Assert
        Assert.Null(jobOffer.SalaryMinMonth);
        Assert.Null(jobOffer.SalaryMaxMonth);
        Assert.Null(jobOffer.SalaryCurrency);
    }
}
