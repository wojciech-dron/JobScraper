using System.Text;
using System.Web;
using JobScraper.Models;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace JobScraper.Scrapers.Indeed;

public class IndeedListScraper : ScrapperBase
{
    private readonly string _baseUrl;
    public IndeedListScraper(IBrowser browser,
        IOptions<ScraperConfig> config,
        ILogger<IndeedListScraper> logger) : base(browser, config, logger)
    {
        _baseUrl = Config.IndeedBaseUrl;
    }

    private string BuildSearchUrl()
    {
        if (!string.IsNullOrEmpty(Config.IndeedSearchUrl))
            return Config.IndeedSearchUrl;

        var encodedJobSearchTerm = HttpUtility.UrlEncode(Config.SearchTerm);

        var urlBuilder = new StringBuilder(_baseUrl)
            .Append("/jobs")
            .Append($"?q={encodedJobSearchTerm}")
            .Append($"&fromage={Config.ListingAgeInDays}");

        if (Config.RemoteJobsOnly)
            urlBuilder.Append("&sc=0kf%3Aattr%28DSQF7%29%3B"); // remote

        if (!string.IsNullOrEmpty(Config.Location))
            urlBuilder.Append($"&l={HttpUtility.UrlEncode(Config.Location)}");

        return urlBuilder.ToString();
    }

    public async IAsyncEnumerable<List<JobOffer>> ScrapeJobs()
    {
        var searchUrl = BuildSearchUrl();
        Logger.LogInformation("Indeed scraping for url {SearchUrl}", searchUrl);

        var page = await LoadUntilAsync(searchUrl, waitSeconds: Config.WaitForListSeconds);

        await SaveScrenshoot(page, $"indeed/list/{DateTime.Now:yyMMdd_HHmm}.png");
        await SavePage(page, $"indeed/list/{DateTime.Now:yyMMdd_HHmm}.html");

        var pageCount = 0;
        while (true)
        {
            pageCount++;
            Logger.LogInformation("Indeed scraping page {PageCount}...", pageCount);

            var scrappedJobs = await ScrapeJobsFromList(page);
            yield return scrappedJobs;

            var nextButton = await page.QuerySelectorAsync("a[data-testid='pagination-page-next']");
            if (nextButton is null)
                break;

            await nextButton.ClickAsync();
            await page.WaitForTimeoutAsync(Config.WaitForListSeconds * 1000);
        }

        Logger.LogInformation("Indeed scraping complete");

    }

    private async Task<List<JobOffer>> ScrapeJobsFromList(IPage indeedPage)
    {
        var jobs = new List<JobOffer>();

        var titleElements = await indeedPage.QuerySelectorAllAsync("h2.jobTitle");
        var titles = await Task.WhenAll(titleElements.Select(async t => await t.InnerTextAsync()));

        var urls = await Task.WhenAll(titleElements.Select(async t =>
        {
            var anchorElement = await t.QuerySelectorAsync("a");
            return anchorElement != null ? await anchorElement.GetAttributeAsync("href") : null;
        }));

        var companyElements = await indeedPage.QuerySelectorAllAsync("[data-testid='company-name']");
        var companyNames =
            await Task.WhenAll(companyElements.Select(async c => await c.InnerTextAsync()));

        var ocationElements = await indeedPage.QuerySelectorAllAsync("[data-testid='text-location']");
        var locations =
            await Task.WhenAll(ocationElements.Select(async l => (await l.InnerTextAsync()).Trim()));

        // Iterating foreach title
        for (var i = 0; i < titles.Length; i++)
        {
            var job = new JobOffer
            {
                Title = titles[i],
                CompanyName = companyNames[i],
                Origin = DataOrigin.Indeed,
                Location = locations[i],
                OfferUrl = _baseUrl + urls[i],
            };

            jobs.Add(job);
        }

        return jobs;
    }
}
