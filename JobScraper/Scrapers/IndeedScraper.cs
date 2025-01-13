using System.Text;
using System.Web;
using JobScraper.Models;
using Microsoft.Playwright;

namespace JobScraper.Scrapers;

public class IndeedScraper
{
    private readonly string _searchUrl;
    private readonly IBrowserContext _context;
    private readonly ScraperConfig _config = new();

    public IndeedScraper(IBrowserContext context)
    {
        var encodedJobSearchTerm = HttpUtility.UrlEncode(_config.SearchTerm);
        var urlBuilder = new StringBuilder("https://pl.indeed.com/jobs")
            .Append($"?q={encodedJobSearchTerm}")
            .Append($"&fromage={_config.ListingAgeInDays}");

        if (_config.RemoteJobsOnly)
            urlBuilder.Append("&sc=0kf%3Aattr%28DSQF7%29%3B"); // remote

        if (!string.IsNullOrEmpty(_config.Location))
            urlBuilder.Append($"&l={HttpUtility.UrlEncode(_config.Location)}");

        _searchUrl = urlBuilder.ToString();
        _context = context;
    }

    public async Task<List<Job>> ScrapeJobsAsync()
    {
        var jobs = new List<Job>();

        var indeedPage = await _context.NewPageAsync();
        await indeedPage.GotoAsync(_searchUrl);
        await indeedPage.WaitForTimeoutAsync(_config.SecondsToWait * 1000);

        var pageCount = 0;

        while (true)
        {
            pageCount++;
            Console.WriteLine($"Indeed scraping page {pageCount}...");

            var scrappedJobs = await ScrapeJobs(indeedPage);
            jobs.AddRange(scrappedJobs);

            break;

            var nextButton = await indeedPage.QuerySelectorAsync("a[data-testid='pagination-page-next']");
            if (nextButton is null)
            {
                Console.WriteLine("Indeed scraping complete.");
                break;
            }


            await nextButton.ClickAsync();
            await indeedPage.WaitForTimeoutAsync(_config.SecondsToWait * 1000);
        }

        foreach (var job in jobs)
        {
            Console.WriteLine($"Scraping job: {job.Title}.");
            await ScrapeDetailsAsync(job);
        }

        return jobs;
    }

    private async Task<List<Job>> ScrapeJobs(IPage indeedPage)
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
                Origin = "Indeed",
                SearchTerm = _config.SearchTerm,
                Title = titles[i],
                CompanyName = companyNames[i],
                OfferUrl = "https://pl.indeed.com" + urls[i],
                Location = locations[i],
                ScrapedAt = DateTime.Now
            };

            if (_config.AvoidJobKeywords.Any(uk => titles[i].Contains(uk, StringComparison.OrdinalIgnoreCase)))
                continue;

            jobs.Add(job);
        }

        return jobs;

    }

    async Task<Job> ScrapeDetailsAsync(Job job)
    {
        var indeedPage = await _context.NewPageAsync();
        await indeedPage.GotoAsync(job.OfferUrl!);

        var indeedJobDescriptionElement = await indeedPage.QuerySelectorAsync("#jobDescriptionText");
        if (indeedJobDescriptionElement != null)
        {
            job.Description = await indeedJobDescriptionElement.InnerTextAsync();
            job.FoundKeywords = _config.Keywords
                .Where(keyword => job.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var externalApplyElement = await indeedPage.QuerySelectorAsync("button[aria-haspopup='dialog']");
        if (externalApplyElement is not null)
        {
            job.ApplyUrl = await externalApplyElement.GetAttributeAsync("href");
            return job;
        }

        var indeedApplyElement = await indeedPage.QuerySelectorAsync(
            "span[data-indeed-apply-joburl], button[href*='https://www.indeed.com/applystart?jk=']");
        if (indeedApplyElement != null)
        {
            job.ApplyUrl = await indeedApplyElement.GetAttributeAsync("data-indeed-apply-joburl");
        }

        return job;
    }
}
