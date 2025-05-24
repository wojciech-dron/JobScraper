using JobScraper.Logic.PracujPl;
using Shouldly;

namespace JobScraper.Tests.Logic.PracujPl;

public class ParseDateTests
{
    [Theory]
    [InlineData("22 maja 2025", 2025, 5, 22)]
    [InlineData("1 lipca 2024", 2024, 7, 1)]
    [InlineData("31 grudnia 2023", 2023, 12, 31)]
    public void ParseDate_ShouldParseCorrectly(string input, int year, int month, int day)
    {
        // Act
        var result = PracujPlListScraper.Handler.ParseDate(input);

        // Assert
        result.ShouldBe(new DateTime(year, month, day));
    }
}