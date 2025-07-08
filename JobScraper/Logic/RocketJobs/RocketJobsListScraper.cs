using System.Text.RegularExpressions;
using JobScraper.Logic.Common;
using JobScraper.Models;
using JobScraper.Persistence;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace JobScraper.Logic.RocketJobs;

public partial class RocketJobsListScraper
{
    public record Command : ScrapeCommand;

    public partial class Handler : ListScraperBase<Command>
    {
        public Handler(IOptions<AppSettings> config,
            ILogger<Handler> logger,
            JobsDbContext dbContext)
            : base(config, logger, dbContext)
        { }

        protected override DataOrigin DataOrigin => DataOrigin.RocketJobs;

        public override async IAsyncEnumerable<List<JobOffer>> ScrapeJobs(SourceConfig sourceConfig)
        {
            var searchUrl = sourceConfig.SearchUrl;
            Logger.LogInformation("{DataOrigin} scraping for url {SearchUrl}", DataOrigin, searchUrl);

            var page = await LoadUntilAsync(searchUrl, waitSeconds: ScrapeConfig.WaitForListSeconds,
                successCondition: async p => (await p.QuerySelectorAllAsync(".offer-card")).Count > 4);

            await AcceptCookies(page);

            var fetchDate = DateTime.UtcNow.ToString("yyMMdd_HHmm");
            var pageNumber = 0;

            await SaveScreenshot(page, $"RocketJobs/list/{fetchDate}/{pageNumber}.png");
            await SavePage(page, $"RocketJobs/list/{fetchDate}/{pageNumber}.html");

            var newJobs = await ScrapeJobsFromList(page);
            yield return newJobs;

            while (newJobs.Count > 0)
            {
                pageNumber++;
                var previousJobs = newJobs;

                Logger.LogInformation("{DataOrigin} - scraping page {PageNumber}", DataOrigin, pageNumber);

                // scroll down
                var scrollHeight = pageNumber * 1200;
                await page.EvaluateAsync($"window.scrollTo(0, {scrollHeight});");
                await page.WaitForTimeoutAsync(ScrapeConfig.WaitForScrollSeconds * 1000);

                await SaveScreenshot(page, $"RocketJobs/list/{fetchDate}/{pageNumber}.png");
                await SavePage(page, $"RocketJobs/list/{fetchDate}/{pageNumber}.html");

                var jobsFromPage = await ScrapeJobsFromList(page);

                var previousOfferUrls = previousJobs.Select(sc => sc.OfferUrl).ToArray();
                newJobs = jobsFromPage.Where(j => !previousOfferUrls.Contains(j.OfferUrl)).ToList();

                yield return newJobs;
            }

            Logger.LogInformation("{DataOrigin} - scrapping complete", DataOrigin);
        }

        private async Task AcceptCookies(IPage page)
        {
            var cookiesAccept = await page.QuerySelectorAsync("#cookiescript_accept");
            if (cookiesAccept is null)
                return;

            Logger.LogInformation("{DataOrigin} - accepting cookies", DataOrigin);
            await cookiesAccept.ClickAsync();

            await page.WaitForTimeoutAsync(1 * 1000);
        }
        private async Task<List<JobOffer>> ScrapeJobsFromList(IPage page)
        {
            var jobs = new List<JobOffer>();

            var jobElements = await page.QuerySelectorAllAsync(".offer-card");
            var jobsText = await Task.WhenAll(jobElements.Select(async j => await j.InnerTextAsync()));

            // Use EvaluateFunctionAsync to get the href attribute of the <a> element
            var urls = await page.EvaluateAsync<string[]>(@"
            Array.from(document.querySelectorAll('.offer-card'))
                .map(element => element.getAttribute('href'));
            ");

            for (var i = 0; i < jobsText.Length; i++)
            {
                var job = new JobOffer();
                // "Senior .NET Developer  20 000 - 24 000 PLN  New  Qodeca  Warszawa  , +9  Locations  Fully remote  C#  Microsoft Azure"
                var phrases = jobsText[i].Split(['\n'], StringSplitOptions.RemoveEmptyEntries).ToList();
                if (phrases.Count < 5)
                    continue;

                job.OfferUrl = BaseUrl + urls.ElementAtOrDefault(i) ?? "";
                job.Title = phrases[0];
                job.Origin = DataOrigin;
                ScrapSalary(job, phrases[1]);
                job.CompanyName = phrases[2];
                job.Location = phrases[3];
                if (phrases[4].Contains(", +"))
                {
                    job.Location += $"{phrases[4]} {phrases[5]}";
                    phrases.RemoveAt(4);
                    phrases.RemoveAt(5);
                }

                job.OfferKeywords = phrases[4..^1];
                job.AgeInfo = phrases.Last();

                jobs.Add(job);
            }

            Logger.LogInformation("{DataOrigin} scraping completed. Total jobs: {JobsCount}", DataOrigin, jobs.Count);

            return jobs;
        }

        private static void ScrapSalary(JobOffer job, string rawSalary)
        {
            if (string.IsNullOrEmpty(rawSalary))
                return;

            rawSalary = rawSalary.Replace(" ", "");

            var minMaxMatch = MinMaxRegex().Match(rawSalary);
            var currencyMatch = CurrencyRegex().Match(rawSalary);
            if (!minMaxMatch.Success || !currencyMatch.Success)
                return;

            job.SalaryMinMonth = int.Parse(minMaxMatch.Groups[1].Value);
            job.SalaryMaxMonth = int.Parse(minMaxMatch.Groups[2].Value);
            job.SalaryCurrency = currencyMatch.Value;
        }

        [GeneratedRegex(@"(\d+)-(\d+)")]
        private static partial Regex MinMaxRegex();

        [GeneratedRegex(@"[A-Z]{3}")]
        private static partial Regex CurrencyRegex();
    }
}