using System.Reflection;
using JobScraper.Web.Common.Entities;
using JobScraper.Web.Common.Models;

namespace JobScraper.Web.Modules.Persistence.Seed.CustomScrapers;

internal static class CustomScraperSeeder
{
    private record ScraperDefinition(int Id, string DataOrigin, string Domain, bool DetailsScrapingEnabled);

    private static readonly Assembly _assembly = typeof(CustomScraperSeeder).Assembly;

    private static readonly ScraperDefinition[] _scrapers =
    [
        new(1, DataOrigins.Indeed, "indeed.com", true),
        new(2, DataOrigins.JustJoinIt, "justjoin.it", true),
        new(3, DataOrigins.NoFluffJobs, "nofluffjobs.com", true),
        new(4, DataOrigins.Olx, "olx.pl", false),
        new(5, DataOrigins.PracujPl, "pracuj.pl", false),
        new(6, DataOrigins.RocketJobs, "rocketjobs.pl", true),
    ];

    public static CustomScraperConfig[] GetData() => _scrapers.Select(scraper => new CustomScraperConfig
    {
        Id = scraper.Id,
        DataOrigin = scraper.DataOrigin,
        Domain = scraper.Domain,
        DetailsScrapingEnabled = scraper.DetailsScrapingEnabled,
        ListScraperScript = ReadScript(scraper.DataOrigin, "list"),
        DetailsScraperScript = scraper.DetailsScrapingEnabled ? ReadScript(scraper.DataOrigin, "details") : null,
        PaginationScript = ReadScript(scraper.DataOrigin, "pagination"),
    }).ToArray();

    private static string ReadScript(string origin, string scriptType)
    {
        var resourceName = $"JobScraper.Web.Modules.Persistence.Seed.CustomScrapers.Scripts.{origin}.{scriptType}.js";
        using var stream = _assembly.GetManifestResourceStream(resourceName)
         ?? throw new InvalidOperationException($"Embedded resource not found: {resourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
