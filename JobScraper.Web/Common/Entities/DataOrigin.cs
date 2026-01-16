namespace JobScraper.Web.Common.Entities;

public enum DataOrigin
{
    Manual,
    Indeed,
    JustJoinIt,
    NoFluffJobs,
    LinkedIn,
    PracujPl,
    RocketJobs,
    Olx,
}

public static class DataOriginHelpers
{
    public static readonly DataOrigin[] Scrapable =
    [
        DataOrigin.PracujPl,
        DataOrigin.Olx,
        DataOrigin.RocketJobs,
        DataOrigin.JustJoinIt,
        DataOrigin.NoFluffJobs,
        DataOrigin.Indeed,
    ];

    public static readonly Dictionary<DataOrigin, string> OriginDomains = new()
    {
        {
            DataOrigin.PracujPl, "pracuj.pl"
        },
        {
            DataOrigin.Olx, "olx.pl"
        },
        {
            DataOrigin.RocketJobs, "rocketjobs.pl"
        },
        {
            DataOrigin.JustJoinIt, "justjoin.it"
        },
        {
            DataOrigin.NoFluffJobs, "nofluffjobs.com"
        },
        {
            DataOrigin.Indeed, "indeed.com"
        },
    };

    public static readonly DataOrigin[] WithDetailsScraping =
    [
        DataOrigin.RocketJobs,
        DataOrigin.JustJoinIt,
        DataOrigin.NoFluffJobs,
        DataOrigin.Indeed,
    ];

    public static bool IsScrapable(this DataOrigin origin) =>
        Scrapable.Contains(origin);

    public static bool HasDetailsScraping(this DataOrigin origin) =>
        WithDetailsScraping.Contains(origin);

    public static DataOrigin? GetDataOriginByUrl(string url)
    {
        foreach (var (key, value) in OriginDomains)
            if (url.Contains(value))
                return key;

        return null;
    }
}
