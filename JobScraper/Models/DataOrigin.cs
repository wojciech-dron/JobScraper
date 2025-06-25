namespace JobScraper.Models;

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

    public static readonly DataOrigin[] WithDetailsScraping =
    [
        DataOrigin.RocketJobs,
        DataOrigin.JustJoinIt,
        DataOrigin.NoFluffJobs,
        DataOrigin.Indeed,
    ];

    public static bool HasDetailsScraping(this DataOrigin origin) =>
        WithDetailsScraping.Contains(origin);
}