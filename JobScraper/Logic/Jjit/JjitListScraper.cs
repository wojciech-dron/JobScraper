using System.Text.RegularExpressions;
using JobScraper.Logic.Common;
using JobScraper.Models;
using JobScraper.Persistence;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace JobScraper.Logic.Jjit;

public class JjitListScraper
{
    public record Command : ScrapeCommand;

    public class Handler : ListScraperBase<Command>
    {
        public Handler(IOptions<ScraperConfig> config,
            ILogger<Handler> logger,
            JobsDbContext dbContext)
            : base(config, logger, dbContext)
        { }

        protected override DataOrigin DataOrigin => DataOrigin.JustJoinIt;

        public override async IAsyncEnumerable<List<JobOffer>> ScrapeJobs()
        {
            if (string.IsNullOrEmpty(SearchUrl))
                throw new ArgumentException("Search URL is not set", nameof(SearchUrl));

            Logger.LogInformation("Justjoin.it scraping for url {SearchUrl}", SearchUrl);

            var page = await LoadUntilAsync(SearchUrl, waitSeconds: ScrapeConfig.WaitForListSeconds,
                successCondition: async p => (await p.QuerySelectorAllAsync("li")).Count > 6);

            await AcceptCookies(page);

            var fetchDate = DateTime.UtcNow.ToString("yyMMdd_HHmm");
            var pageNumber = 0;

            await SaveScreenshot(page, $"jjit/list/{fetchDate}/{pageNumber}.png");
            await SavePage(page, $"jjit/list/{fetchDate}/{pageNumber}.html");

            var newJobs = await ScrapeJobsFromList(page);
            yield return newJobs;

            while (newJobs.Count > 0)
            {
                pageNumber++;
                var previousJobs = newJobs;

                Logger.LogInformation("Justjoin.it - scraping page {PageNumber}", pageNumber);

                // scroll down
                var scrollHeight = pageNumber * 1200;
                await page.EvaluateAsync($"window.scrollTo(0, {scrollHeight});");
                await page.WaitForTimeoutAsync(ScrapeConfig.WaitForScrollSeconds * 1000);

                await SaveScreenshot(page, $"jjit/list/{fetchDate}/{pageNumber}.png");
                await SavePage(page, $"jjit/list/{fetchDate}/{pageNumber}.html");

                var jobsFromPage = await ScrapeJobsFromList(page);

                var previousOfferUrls = previousJobs.Select(sc => sc.OfferUrl).ToArray();
                newJobs = jobsFromPage.Where(j => !previousOfferUrls.Contains(j.OfferUrl)).ToList();

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

                job.OfferUrl = BaseUrl + urls.ElementAtOrDefault(i) ?? "";
                job.Title = phrases[0];
                job.Origin = DataOrigin.JustJoinIt;
                ScrapSalary(job, phrases[1]);
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

        private static void ScrapSalary(JobOffer job, string rawSalary)
        {
            if (string.IsNullOrEmpty(rawSalary))
                return;

            rawSalary = rawSalary.Replace(" ", "");

            var minMaxMatch = Regex.Match(rawSalary, @"(\d+)-(\d+)");
            var currencyMatch = Regex.Match(rawSalary, @"[A-Z]{3}");
            if (!minMaxMatch.Success || !currencyMatch.Success)
                return;

            job.SalaryMinMonth = int.Parse(minMaxMatch.Groups[1].Value);
            job.SalaryMaxMonth = int.Parse(minMaxMatch.Groups[2].Value);
            job.SalaryCurrency = currencyMatch.Value;
        }
    }
}