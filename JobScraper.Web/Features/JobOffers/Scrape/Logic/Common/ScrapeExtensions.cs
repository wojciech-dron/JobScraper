namespace JobScraper.Web.Features.JobOffers.Scrape.Logic.Common;

public static class ScrapeExtensions
{
    public static DateTime? TryParseDate(this string? date) =>
        DateTime.TryParse(date, out var result) ? result : null;
}
