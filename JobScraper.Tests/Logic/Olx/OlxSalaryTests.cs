using JobScraper.Logic.Olx;
using JobScraper.Models;
using Shouldly;

namespace JobScraper.Tests.Logic.Olx;

public class OlxSalaryTests
{
    [Theory]
    [InlineData("4 666 - 7 000 zł / mies. brutto", 3793, 5691, "zł")]
    [InlineData("32 - 38 zł / godz. brutto", 4162, 4943, "zł")]
    [InlineData("2 333 - 2 335 zł / mies. brutto", 1896, 1898, "zł")]
    [InlineData("99 - 100 zł / godz. brutto", 12878, 13008, "zł")]
    [InlineData("30,50 - 31 zł / godz. brutto", 3967, 4032, "zł")]
    [InlineData("30,50 - 35 zł / godz. brutto", 3967, 4552, "zł")]
    [InlineData("99 - 100 zł / godz. brutto", 12878, 13008, "zł")]
    [InlineData("9 000 - 24 000 zł / mies. brutto", 7317, 19512, "zł")]
    [InlineData("6 000 - 20 000 zł / mies. brutto", 4878, 16260, "zł")]
    [InlineData("30,30 - 60 zł / godz. brutto", 3941, 7804, "zł")]
    [InlineData("4 670 - 5 650 zł / mies. brutto", 3796, 4593, "zł")]
    [InlineData("6 000 - 7 000 zł / mies. brutto", 4878, 5691, "zł")]
    [InlineData("5 000 - 5 363 zł / mies. brutto", 4065, 4360, "zł")]
    [InlineData("4 000 - 6 000 zł / mies. brutto", 3252, 4878, "zł")]
    [InlineData("35 - 40 zł / godz. brutto", 4552, 5203, "zł")]
    [InlineData("5 000 - 5 500 zł / mies. brutto", 4065, 4471, "zł")]
    [InlineData("20 - 30 zł / godz. brutto", 2601, 3902, "zł")]
    [InlineData("7 000 zł / mies. brutto", 5691, 5691, "zł")]
    [InlineData("6 000 - 10 000 zł / mies. brutto", 4878, 8130, "zł")]
    [InlineData("6 500 - 8 200 zł / mies. brutto", 5284, 6666, "zł")]
    [InlineData("4 800 - 8 760 zł / mies. brutto", 3902, 7121, "zł")]
    [InlineData("30 - 35 zł / godz. brutto", 3902, 4552, "zł")]
    [InlineData("9 000 - 12 000 zł / mies. brutto", 7317, 9756, "zł")]
    [InlineData("100 - 150 zł / godz. brutto", 13008, 19512, "zł")]
    [InlineData("6 300 - 7 400 zł / mies. brutto", 5121, 6016, "zł")]
    [InlineData("30,50 - 35 zł / godz. brutto", 3967, 4552, "zł")]
    [InlineData("4 666 - 6 000 zł / mies. brutto", 3793, 4878, "zł")]
    [InlineData("7 700 - 7 800 zł / mies. brutto", 6260, 6341, "zł")]
    [InlineData("9 000 - 12 500 zł / mies. brutto", 7317, 10162, "zł")]
    [InlineData("4 000 - 5 000 zł / mies. brutto", 3252, 4065, "zł")]
    public void SetSalary_AllExamples_SetsCorrectValues(string rawSalary, int expectedMin, int expectedMax, string expectedCurrency)
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
    public void SetSalary_EmptySalary_DoesNotSetValues()
    {
        // Arrange
        var jobOffer = new JobOffer();
        var rawSalary = "";

        // Act
        var isParsed = SalaryParser.TryParseSalary(jobOffer, rawSalary);

        // Assert
        isParsed.ShouldBe(false);
        jobOffer.SalaryMinMonth.ShouldBeNull();
        jobOffer.SalaryMaxMonth.ShouldBeNull();
        jobOffer.SalaryCurrency.ShouldBeNull();
    }

    [Fact]
    public void SetSalary_InvalidFormat_DoesNotSetValues()
    {
        // Arrange
        var jobOffer = new JobOffer();
        var rawSalary = "Invalid salary format";

        // Act
        var isParsed = SalaryParser.TryParseSalary(jobOffer, rawSalary);

        // Assert
        isParsed.ShouldBe(false);
        jobOffer.SalaryMinMonth.ShouldBeNull();
        jobOffer.SalaryMaxMonth.ShouldBeNull();
        jobOffer.SalaryCurrency.ShouldBeNull();
    }
}
