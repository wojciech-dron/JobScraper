using System.Text;
using System.Web;
using JobScraper.Models;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace JobScraper.Scrapers;

public class JjitListScraper : ScrapperBase
{
    private readonly string _baseUrl;
    public JjitListScraper(IBrowser browser,
        IOptions<ScraperConfig> config,
        ILogger<JjitListScraper> logger) : base(browser, config, logger)
    {
        _baseUrl = Config.JjitBaseUrl;
    }

    private string BuildSearchUrl()
    {
        var urlBuilder = new StringBuilder(_baseUrl)
            .Append("/job-offers/all-locations/net");

        if (Config.RemoteJobsOnly)
            urlBuilder.Append("?remote=yes");

        return urlBuilder.ToString();
    }

    public async Task<List<Job>> ScrapeJobs()
    {
        var searchUrl = BuildSearchUrl();
        Logger.LogInformation("Justjoin.it scraping for url {SearchUrl}", searchUrl);

        var page = await LoadUntilAsync(searchUrl, waitSeconds: Config.WaitForListSeconds,
            successCondition: async p => (await p.QuerySelectorAllAsync("li")).Count > 10);

        await AcceptCookies(page);
        await ScrollUntilAllJobsAreLoaded(page);

        await SaveScrenshoot(page, $"jobs\\jjit-list{DateTime.Now:yyMMdd_HHmm}.png");
        await SavePage(page, $"jobs\\jjit-list{DateTime.Now:yyMMdd_HHmm}.html");

        Logger.LogInformation("Justjoin.it - page ready, scraping jobs");
        var scrappedJobs = await ScrapeJobsFromList(page);

        return scrappedJobs;
    }

    private async Task AcceptCookies(IPage page)
    {
        var cookiesAccept = await page.QuerySelectorAsync("#cookiescript_accept");
        if (cookiesAccept is null)
            return;

        Logger.LogInformation("Justjoin.it - accepting cookies");
        await cookiesAccept.ClickAsync();

        await page.WaitForTimeoutAsync(1 * 1000);
    }

    private async Task<IPage> ScrollUntilAllJobsAreLoaded(IPage page)
    {
        var maxRetries = 10;
        var retryCount = 0;
        do
        {
            var element = await page.QuerySelectorAsync(
                "text=Add an e-mail notification, and we will inform you about new job offers according to the given criteria.");

            if (element is not null)
                return page;

            Logger.LogInformation("Justjoin.it - scrolling down the page");

            // scroll down the page
            await page.EvaluateAsync("window.scrollTo(0, document.body.scrollHeight);");
            await page.WaitForTimeoutAsync(3 * 1000);
            retryCount++;
        } while (retryCount < maxRetries);

        throw new ApplicationException($"Failed to load all jobs after {retryCount} retries");
    }

    private async Task<List<Job>> ScrapeJobsFromList(IPage page)
    {
        var jobs = new List<Job>();

        var jobElements = await page.QuerySelectorAllAsync("li[data-index]");
        var jobsText = await Task.WhenAll(jobElements.Select(async j => await j.InnerTextAsync()));

        var urlElements = await page.QuerySelectorAllAsync("a.offer_list_offer_link");
        var urls = await Task.WhenAll(urlElements.Select(async t => await t.GetAttributeAsync("href")));

        // Iterating foreach title
        for (var i = 0; i < jobsText.Length; i++)
        {
            var job = new Job();
            // "Senior .NET Developer  20 000 - 24 000 PLN  New  Qodeca  Warszawa  , +9  Locations  Fully remote  C#  Microsoft Azure"
            var phrases = jobsText[i].Split(['\n'], StringSplitOptions.RemoveEmptyEntries).ToList();
            if (phrases.Count < 9)
                continue;

            job.OfferUrl = _baseUrl + urls.ElementAtOrDefault(i) ?? "";
            job.Title = phrases[0];
            job.Origin = "Justjoin.it";
            job.Salary = phrases[1];
            job.AgeInfo = phrases[2];
            job.CompanyName = phrases[3];
            job.Location = phrases[4];
            if (phrases[6].Contains("Locations"))
            {
                job.Location += $"{phrases[5]} {phrases[6]}";
                phrases.RemoveAt(5);
                phrases.RemoveAt(6);
            }

            job.OfferKeywords = phrases.Skip(6).ToList();

            jobs.Add(job);
        }

        Logger.LogInformation("Justjoin.it scraping completed. Total jobs: {JobsCount}", jobs.Count);

        return jobs;
    }
}
