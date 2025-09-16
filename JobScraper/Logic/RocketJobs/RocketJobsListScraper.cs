using System.Text.Json;
using System.Text.RegularExpressions;
using JobScraper.Extensions;
using JobScraper.Logic.Common;
using JobScraper.Models;
using JobScraper.Persistence;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace JobScraper.Logic.RocketJobs;

public class RocketJobsListScraper
{
    public record Command : ScrapeCommand;

    public class Handler : ListScraperBase<Command>
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
                successCondition: async p => (await p.QuerySelectorAllAsync("li")).Count > 6);

            await AcceptCookies(page);

            var fetchDate = DateTime.UtcNow.ToString("yyMMdd_HHmm");
            var pageNumber = 0;

            await SaveScreenshot(page, $"{DataOrigin}/list/{fetchDate}/{pageNumber}.png");
            await SavePage(page, $"{DataOrigin}/list/{fetchDate}/{pageNumber}.html");

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

                await SaveScreenshot(page, $"rocketJobs/list/{fetchDate}/{pageNumber}.png");
                await SavePage(page, $"rocketJobs/list/{fetchDate}/{pageNumber}.html");

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

        record JobData(
            string Title,
            string Url,
            string CompanyName,
            string Location,
            string Salary,
            List<string> OfferKeywords
        );

        private async Task<List<JobOffer>> ScrapeJobsFromList(IPage page)
        {
            var script = await ScrapeHelpers.GetJsScript("JobScraper.Logic.Jjit.jjit-list.js"); // yes, jjit, same layout
            var result = await page.EvaluateAsync<string>(script);
            var scrapedOffers = JsonSerializer.Deserialize<JobData[]>(result)!;

            var jobs = new List<JobOffer>();
            foreach (var data in scrapedOffers)
            {
                var jobOffer = new JobOffer
                {
                    Title = data.Title,
                    OfferUrl = BaseUrl + data.Url,
                    CompanyName = data.CompanyName,
                    Location = data.Location,
                    OfferKeywords = data.OfferKeywords,
                    Origin = DataOrigin,
                    DetailsScrapeStatus = DetailsScrapeStatus.ToScrape,
                };

                SetSalary(jobOffer, data.Salary);
                jobOffer.SetDefaultDescription();

                jobs.Add(jobOffer);
            }

            Logger.LogInformation("{DataOrigin} scraping completed. Total jobs: {JobsCount}", DataOrigin, jobs.Count);

            return jobs;
        }

        // 20 000 - 26 000 PLN/month
        // 100 - 130 PLN/h
        private static void SetSalary(JobOffer job, string rawSalary)
        {
            if (string.IsNullOrEmpty(rawSalary))
                return;

            rawSalary = rawSalary.Replace(" ", "");

            var minMaxMatch = Regex.Match(rawSalary, @"(\d+)-(\d+)");
            var currencyMatch = Regex.Match(rawSalary, @"[A-Z]{3}");
            if (!minMaxMatch.Success || !currencyMatch.Success)
                return;

            var period = SalaryPeriod.Month;
            if (rawSalary.Contains("h")) period = SalaryPeriod.Hour;

            job.SalaryMinMonth = int.Parse(minMaxMatch.Groups[1].Value).ApplyMonthPeriod(period);
            job.SalaryMaxMonth = int.Parse(minMaxMatch.Groups[2].Value).ApplyMonthPeriod(period);
            job.SalaryCurrency = currencyMatch.Value;
        }
    }
}