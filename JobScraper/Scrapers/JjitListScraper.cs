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
        if (!string.IsNullOrEmpty(Config.JjitSearchUrl))
            return Config.JjitSearchUrl;

        var urlBuilder = new StringBuilder(_baseUrl)
            .Append("/job-offers/all-locations/net");

        if (Config.RemoteJobsOnly)
            urlBuilder.Append("?remote=yes");

        return urlBuilder.ToString();
    }

    public async IAsyncEnumerable<List<JobOffer>> ScrapeJobs()
    {
        var searchUrl = BuildSearchUrl();
        Logger.LogInformation("Justjoin.it scraping for url {SearchUrl}", searchUrl);

        var page = await LoadUntilAsync(searchUrl, waitSeconds: Config.WaitForListSeconds,
            successCondition: async p => (await p.QuerySelectorAllAsync("li")).Count > 6);

        await AcceptCookies(page);

        await SaveScrenshoot(page, $"jjit/list/{DateTime.Now:yyMMdd_HHmm}.png");
        await SavePage(page, $"jjit/list/{DateTime.Now:yyMMdd_HHmm}.html");

        var newJobs = await ScrapeJobsFromList(page);
        yield return newJobs;

        var pageNumber = 0;
        while (newJobs.Count > 0)
        {
            pageNumber++;
            var previousJobs = newJobs;

            Logger.LogInformation("Justjoin.it - scraping page {PageNumber}", pageNumber);

            // scroll down
            var scrollHeight = pageNumber * 1200;
            await page.EvaluateAsync($"window.scrollTo(0, {scrollHeight});");
            await page.WaitForTimeoutAsync(Config.WaitForScrollSeconds * 1000);

            await SaveScrenshoot(page, $"jjit/list/{pageNumber}-{DateTime.Now:yyMMdd_HHmm}.png");
            await SavePage(page, $"jjit/list/{pageNumber}-{DateTime.Now:yyMMdd_HHmm}.html");

            var jobsFromPage = await ScrapeJobsFromList(page);

            newJobs = jobsFromPage.Where(j =>
                    !previousJobs.Select(sc => sc.OfferUrl).Contains(j.OfferUrl))
                .ToList();

            yield return newJobs;
        }

        Logger.LogInformation("Justjoin.it - scrapping complete");
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
    private async Task<List<JobOffer>> ScrapeJobsFromList(IPage page)
    {
        var jobs = new List<JobOffer>();

        var jobElements = await page.QuerySelectorAllAsync("div[data-index]");
        var jobsText = await Task.WhenAll(jobElements.Select(async j => await j.InnerTextAsync()));

        // Use EvaluateFunctionAsync to get the href attribute of the <a> element
        var urls = await page.EvaluateAsync<string[]>(@"
            Array.from(document.querySelectorAll('div[data-index] > div > div > a'))
                .map(element => element.getAttribute('href'));
        ");

        for (var i = 0; i < jobsText.Length; i++)
        {
            var job = new JobOffer();
            // "Senior .NET Developer  20 000 - 24 000 PLN  New  Qodeca  Warszawa  , +9  Locations  Fully remote  C#  Microsoft Azure"
            var phrases = jobsText[i].Split(['\n'], StringSplitOptions.RemoveEmptyEntries).ToList();
            if (phrases.Count < 9)
                continue;

            job.OfferUrl = _baseUrl + urls.ElementAtOrDefault(i) ?? "";
            job.Title = phrases[0];
            job.Origin = "Justjoin.it";
            job.Salary = phrases[1];
            job.AgeInfo = phrases[2];

            var company = new Company();
            company.Name = phrases[3];
            job.Company = company;

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
