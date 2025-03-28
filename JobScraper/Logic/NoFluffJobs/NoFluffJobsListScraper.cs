using System.Text.RegularExpressions;
using JobScraper.Logic.Common;
using JobScraper.Models;
using JobScraper.Persistence;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace JobScraper.Logic.NoFluffJobs;

public class NoFluffJobsListScraper
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
                throw new ArgumentException("SearchUrl is null or empty", nameof(SearchUrl));

            Logger.LogInformation("NoFluffJobs scraping for url {SearchUrl}", SearchUrl);

            var page = await LoadUntilAsync(SearchUrl, waitSeconds: ScrapeConfig.WaitForListSeconds);

            var fetchDate = DateTime.UtcNow.ToString("yyMMdd_HHmm");
            var pageNumber = 0;

            await SaveScrenshoot(page, $"NoFluffJobs/list/{fetchDate}/{pageNumber}.png");
            await SavePage(page, $"NoFluffJobs/list/{fetchDate}/{pageNumber}.html");

            var newJobs = await ScrapeJobsFromList(page);
            var previousJobs = new List<JobOffer>();
            yield return newJobs;

            while (newJobs.Count > 0)
            {
                pageNumber++;
                previousJobs.AddRange(newJobs);

                Logger.LogInformation("NoFluffJobs - scraping page {PageNumber}", pageNumber);

                var nextButton = await page.QuerySelectorAsync("button.tw-btn.tw-btn-primary.tw-px-8.tw-block.tw-btn-xl");

                if (nextButton is null)
                    break;

                await nextButton.ClickAsync();
                await page.WaitForTimeoutAsync(ScrapeConfig.WaitForListSeconds * 1000);

                await SaveScrenshoot(page, $"NoFluffJobs/list/{fetchDate}/{pageNumber}.png");
                await SavePage(page, $"NoFluffJobs/list/{fetchDate}/{pageNumber}.html");

                var jobsFromPage = await ScrapeJobsFromList(page);

                var previousOfferUrls = previousJobs.Select(sc => sc.OfferUrl).ToArray();
                newJobs = jobsFromPage.Where(j => !previousOfferUrls.Contains(j.OfferUrl)).ToList();

                yield return newJobs;
            }

            Logger.LogInformation("NoFluffJobs - scrapping complete");
        }

        private async Task<List<JobOffer>> ScrapeJobsFromList(IPage page)
        {
            var titles = await page.EvaluateAsync<string[]>(
                "Array.from(document.querySelectorAll('nfj-posting-item-title > header > h3')).map(x => x.textContent)");

            var urls = await page.EvaluateAsync<string[]>(
                "Array.from(document.querySelectorAll('a.posting-list-item')).map(x => x.getAttribute('href'))");

            var salaries = await page.EvaluateAsync<string[]>(
                "Array.from(document.querySelectorAll('nfj-posting-item-salary')).map(x => x.textContent)");

            var jobKeys = await page.EvaluateAsync<string[]>(
                "Array.from(document.querySelectorAll('nfj-posting-item-tiles')).map(x => x.textContent)");

            var companyNames = await page.EvaluateAsync<string[]>(
                "Array.from(document.querySelectorAll('aside.tw-w-full > footer > h4')).map(x => x.textContent)");

            var locations = await page.EvaluateAsync<string[]>(
                "Array.from(document.querySelectorAll('nfj-posting-item-city')).map(x => x.textContent)");

            var jobs = new List<JobOffer>();
            for (var i = 0; i < titles.Length; i++)
            {
                var title = titles[i].Replace("NOWA", "").Trim();
                var url = BaseUrl + urls[i];
                var salary = salaries[i].Trim('\n').Replace("\u00a0", "").Replace(" ", "");
                var offerJobKeys = jobKeys[i].Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();
                var companyName = companyNames[i].Trim();
                var location = locations[i].Replace("  ", " ").Replace("  ", " ").Trim();

                var jobOffer = new JobOffer
                {
                    Title = title,
                    OfferUrl = url,
                    OfferKeywords = offerJobKeys,
                    CompanyName = companyName,
                    Location = location,
                    Origin = DataOrigin.NoFluffJobs
                };

                SetSalary(jobOffer, salary);

                jobs.Add(jobOffer);
            }

            return jobs;
        }

        private void SetSalary(JobOffer jobOffer, string salary)
        {
            // Example: 18000–24000PLN

            var match = Regex.Match(salary, @"^(\d+)–(\d+)([A-Z]+)$");

            if (!match.Success)
                return;

            jobOffer.SalaryMinMonth = int.Parse(match.Groups[1].Value);
            jobOffer.SalaryMaxMonth = int.Parse(match.Groups[2].Value);
            jobOffer.SalaryCurrency = match.Groups[3].Value;
        }
    }
}
