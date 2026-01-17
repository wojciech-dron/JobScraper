using System.Text.Json;
using System.Text.RegularExpressions;
using JobScraper.Web.Common.Entities;
using JobScraper.Web.Features.JobOffers.Scrape.Logic.Common;
using JobScraper.Web.Features.JobOffers.Scrape.Logic.Extensions;
using JobScraper.Web.Modules.Persistence;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace JobScraper.Web.Features.JobOffers.Scrape.Logic.NoFluffJobs;

public partial class NoFluffJobsListScraper
{
    public record Command(SourceConfig Source) : ScrapeCommand(Source);

    public partial class Handler : ListScraperBase<Command>
    {

        protected override DataOrigin DataOrigin => DataOrigin.NoFluffJobs;
        public Handler(IOptions<AppSettings> config,
            ILogger<Handler> logger,
            JobsDbContext dbContext)
            : base(config, logger, dbContext)
        { }

        public override async IAsyncEnumerable<List<JobOffer>> ScrapeJobs(SourceConfig sourceConfig)
        {
            var searchUrl = sourceConfig.SearchUrl;
            ArgumentException.ThrowIfNullOrWhiteSpace(searchUrl);

            Logger.LogInformation("{DataOrigin} scraping for url {SearchUrl}", DataOrigin, searchUrl);

            var page = await LoadUntilAsync(searchUrl, waitSeconds: ScrapeConfig.WaitForListSeconds);

            var fetchDate = DateTime.UtcNow.ToString("yyMMdd_HHmm");
            var pageNumber = 0;

            await SaveScreenshot(page, $"{DataOrigin}/list/{fetchDate}/{pageNumber}.png");
            await SavePage(page, $"{DataOrigin}/list/{fetchDate}/{pageNumber}.html");

            var newJobs = await ScrapeJobsFromList(page);
            var previousJobs = new List<JobOffer>();
            yield return newJobs;

            while (newJobs.Count > 0)
            {
                pageNumber++;
                previousJobs.AddRange(newJobs);

                Logger.LogInformation("{DataOrigin} - scraping page {PageNumber}", DataOrigin, pageNumber);

                var nextButton = await page.EvaluateAsync<bool>(
                    """
                    () => {
                        const button = document.querySelector('button[nfjloadmore]');
                        if (!button) {
                            return false;
                        }

                        button.click();
                        return true;
                    }
                    """);

                if (!nextButton)
                    break;

                await page.WaitForTimeoutAsync(ScrapeConfig.WaitForListSeconds * 1000);

                await SaveScreenshot(page, $"{DataOrigin}/list/{fetchDate}/{pageNumber}.png");
                await SavePage(page, $"{DataOrigin}/list/{fetchDate}/{pageNumber}.html");

                var jobsFromPage = await ScrapeJobsFromList(page);

                var previousOfferUrls = previousJobs.Select(sc => sc.OfferUrl).ToArray();
                newJobs = jobsFromPage.Where(j => !previousOfferUrls.Contains(j.OfferUrl)).ToList();

                yield return newJobs;
            }

            Logger.LogInformation("{DataOrigin} - scrapping complete", DataOrigin);
        }

        private async Task<List<JobOffer>> ScrapeJobsFromList(IPage page)
        {
            var script = await ScrapeHelpers.GetJsScript(
                "JobScraper.Web.Features.JobOffers.Scrape.Logic.NoFluffJobs.no-fluff-jobs-list.js");
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

            return jobs;
        }

        private static void SetSalary(JobOffer jobOffer, string salary)
        {
            // Example: 19 000  – 24 000  PLN
            // Example: 18000–24000PLN
            var match = SalaryRegex().Match(salary.Replace(" ", "").Replace(" ", ""));

            if (!match.Success)
                return;

            jobOffer.SalaryMinMonth = int.Parse(match.Groups[1].Value);
            jobOffer.SalaryMaxMonth = int.Parse(match.Groups[2].Value);
            jobOffer.SalaryCurrency = match.Groups[3].Value;
        }

        [GeneratedRegex(@"^(\d+)–(\d+)([A-Z]+)$")]
        private static partial Regex SalaryRegex();

        private record JobData(
            string Title,
            string Url,
            string CompanyName,
            string Location,
            string Salary,
            List<string> OfferKeywords
        );
    }
}
