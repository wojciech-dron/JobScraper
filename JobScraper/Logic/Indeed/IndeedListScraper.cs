﻿using System.Text;
using System.Web;
using JobScraper.Logic.Common;
using JobScraper.Models;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace JobScraper.Logic.Indeed;

public class IndeedListScraper : ScrapperBase
{
    private readonly string _baseUrl;
    protected override DataOrigin DataOrigin => DataOrigin.Indeed;

    public IndeedListScraper(
        IOptions<ScraperConfig> config,
        ILogger<IndeedListScraper> logger) : base(config, logger)
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

        var fetchDate = DateTime.UtcNow.ToString("yyMMdd_HHmm");
        var pageNumber = 0;

        await SaveScrenshoot(page, $"indeed/list/{fetchDate}/{pageNumber}.png");
        await SavePage(page, $"indeed/list/{fetchDate}/{pageNumber}.html");
        while (true)
        {
            pageNumber++;
            Logger.LogInformation("Indeed scraping page {PageCount}...", pageNumber);

            var scrappedJobs = await ScrapeJobsFromList(page);
            yield return scrappedJobs;

            var nextButton = await page.QuerySelectorAsync("a[data-testid='pagination-page-next']");
            if (nextButton is null)
                break;

            await nextButton.ClickAsync();
            await page.WaitForTimeoutAsync(Config.WaitForListSeconds * 1000);

            await SaveScrenshoot(page, $"jjit/list/{fetchDate}/{pageNumber}.png");
            await SavePage(page, $"jjit/list/{fetchDate}/{pageNumber}.html");
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

        var locationElements = await indeedPage.QuerySelectorAllAsync("[data-testid='text-location']");
        var locations =
            await Task.WhenAll(locationElements.Select(async l => (await l.InnerTextAsync()).Trim()));

        // Iterating foreach title
        for (var i = 0; i < titles.Length; i++)
        {
            var offerUrl = _baseUrl + urls[i]!.Split('&')[0].Replace("/rc/clk", "/viewjob");
            var job = new JobOffer
            {
                Title = titles[i],
                CompanyName = companyNames[i],
                Origin = DataOrigin.Indeed,
                Location = locations[i],
                OfferUrl = offerUrl,
            };

            jobs.Add(job);
        }

        return jobs;
    }
}
