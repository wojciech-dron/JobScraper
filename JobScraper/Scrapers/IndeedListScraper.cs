using System.Text;
using System.Web;
using JobScraper.Models;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace JobScraper.Scrapers;

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

    public async Task<List<Job>> ScrapeJobs()
    {
        var jobs = new List<Job>();
        var searchUrl = BuildSearchUrl();
        Logger.LogInformation("Indeed scraping for url {SearchUrl}", searchUrl);

        var indeedPage = await LoadUntilAsync(searchUrl, waitSeconds: Config.WaitForListSeconds);

        var pageCount = 0;
        while (true)
        {
            pageCount++;
            Logger.LogInformation("Indeed scraping page {PageCount}...", pageCount);

            var scrappedJobs = await ScrapeJobsFromList(indeedPage);
            jobs.AddRange(scrappedJobs);

            var nextButton = await indeedPage.QuerySelectorAsync("a[data-testid='pagination-page-next']");
            if (nextButton is null)
            {
                Logger.LogInformation("Indeed scraping complete");
                break;
            }


            await nextButton.ClickAsync();
            await indeedPage.WaitForTimeoutAsync(Config.WaitForListSeconds * 1000);
        }

        return jobs;
    }

    private async Task<List<Job>> ScrapeJobsFromList(IPage indeedPage)
    {
        var jobs = new List<Job>();

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
            var job = new Job
            {
                Title = titles[i],
                CompanyName = companyNames[i],
                Origin = "Indeed",
                Location = locations[i],
                OfferUrl = _baseUrl + urls[i],
            };

            jobs.Add(job);
        }

        return jobs;
    }
}
