using System.Globalization;

namespace JobScraper.IntegrationTests.Utils;

public static class DateTimeExtensions
{
    extension(DateTime)
    {
        public static DateTime ParseUtc(string dateString) => DateTime.Parse(dateString,
            null,
            DateTimeStyles.AssumeUniversal |
            DateTimeStyles.AdjustToUniversal);
    }
}
