namespace JobScraper.Web.Common.Entities;

public static class DataOrigins
{
    public const string Manual = "Manual";
    public const string Indeed = "Indeed";
    public const string JustJoinIt = "JustJoinIt";
    public const string NoFluffJobs = "NoFluffJobs";
    public const string LinkedIn = "LinkedIn";
    public const string PracujPl = "PracujPl";
    public const string RocketJobs = "RocketJobs";
    public const string Olx = "Olx";
}

public static class DataOriginHelpers
{
    public static readonly string[] Scrapable =
    [
        DataOrigins.PracujPl,
        DataOrigins.Olx,
        DataOrigins.RocketJobs,
        DataOrigins.JustJoinIt,
        DataOrigins.NoFluffJobs,
        DataOrigins.Indeed,
    ];

    public static readonly Dictionary<string, string> OriginDomains = new()
    {
        {
            DataOrigins.PracujPl, "pracuj.pl"
        },
        {
            DataOrigins.Olx, "olx.pl"
        },
        {
            DataOrigins.RocketJobs, "rocketjobs.pl"
        },
        {
            DataOrigins.JustJoinIt, "justjoin.it"
        },
        {
            DataOrigins.NoFluffJobs, "nofluffjobs.com"
        },
        {
            DataOrigins.Indeed, "indeed.com"
        },
    };

    public static readonly string[] WithDetailsScraping =
    [
        DataOrigins.RocketJobs,
        DataOrigins.JustJoinIt,
        DataOrigins.NoFluffJobs,
        DataOrigins.Indeed,
    ];

    public static bool IsScrapable(this string origin) =>
        Scrapable.Contains(origin);

    public static bool HasDetailsScraping(this string origin) =>
        WithDetailsScraping.Contains(origin);

    public static string? GetDataOriginByUrl(string url)
    {
        foreach (var (key, value) in OriginDomains)
            if (url.Contains(value))
                return key;

        return null;
    }
}
