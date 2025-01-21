namespace JobScraper.Scrapers;

public static class ScrapeExtensions
{
    public static DateTime? TryParseDate(this string? date) =>
        DateTime.TryParse(date, out var result) ? result : null;
}