using JobScraper.IntegrationTests.Host.Services;
using JobScraper.Web.Common.Entities;

namespace JobScraper.IntegrationTests.Factories;

public static class ScraperConfigFactory
{
    public static ScraperConfig CreateScraperConfig(this ObjectMother objectMother,
        string owner = "test@email.com",
        List<string>? myKeywords = null,
        List<string>? avoidKeywords = null,
        bool starMyKeywords = false,
        float waitForListSeconds = 10,
        float waitForDetailsSeconds = 5,
        bool showBrowserWhenScraping = false)
    {
        var entity = new ScraperConfig
        {
            Owner = owner,
            MyKeywords = myKeywords ?? [],
            AvoidKeywords = avoidKeywords ?? [],
            StarMyKeywords = starMyKeywords,
            WaitForListSeconds = waitForListSeconds,
            WaitForDetailsSeconds = waitForDetailsSeconds,
            ShowBrowserWhenScraping = showBrowserWhenScraping,
        };

        objectMother.Add(entity);

        return entity;
    }

    public static CustomScraperConfig CreateCustomScraperConfig(this ObjectMother objectMother,
        string owner = "test@email.com",
        string dataOrigin = "TestSite",
        string listScraperScript = "() => JSON.stringify([])",
        bool detailsScrapingEnabled = false,
        string? detailsScraperScript = null,
        string? paginationScript = null,
        string domain = "")
    {
        var entity = new CustomScraperConfig
        {
            Owner = owner,
            DataOrigin = dataOrigin,
            ListScraperScript = listScraperScript,
            DetailsScrapingEnabled = detailsScrapingEnabled,
            DetailsScraperScript = detailsScraperScript,
            PaginationScript = paginationScript,
            Domain = domain,
        };

        objectMother.Add(entity);

        return entity;
    }
}
