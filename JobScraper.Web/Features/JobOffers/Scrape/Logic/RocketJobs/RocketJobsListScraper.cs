using System.Text.Json;
using JobScraper.Web.Common.Entities;
using JobScraper.Web.Features.JobOffers.Scrape.Logic.Common;
using JobScraper.Web.Features.JobOffers.Scrape.Logic.Extensions;
using JobScraper.Web.Modules.Persistence;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace JobScraper.Web.Features.JobOffers.Scrape.Logic.RocketJobs;

public partial class RocketJobsListScraper
{
    public record Command(SourceConfig Source) : ScrapeCommand(Source);

    public partial class Handler(
        IOptions<AppSettings> config,
        ILogger<Handler> logger,
        JobsDbContext dbContext)
        : ListScraperBaseHandler<Command>(config, logger, dbContext)
    {
        public override async IAsyncEnumerable<List<JobOffer>> ScrapeJobs(SourceConfig sourceConfig)
        {
            var searchUrl = sourceConfig.SearchUrl;
            var page = await LoadUntilAsync(searchUrl,
                waitSeconds: ScrapeConfig.WaitForListSeconds,
                successCondition: async p => (await p.QuerySelectorAllAsync("li")).Count > 6);

            await AcceptCookies(page);

            var fetchDate = DateTime.UtcNow.ToString("yyMMdd_HHmm");
            var pageNumber = 1;

            await SaveScreenshot(page, $"{Origin}/list/{fetchDate}/{pageNumber}.png");
            await SavePage(page, $"{Origin}/list/{fetchDate}/{pageNumber}.html");

            var newJobs = await ScrapeJobsFromList(page);
            yield return newJobs;

            while (newJobs.Count > 0)
            {
                pageNumber++;
                var previousJobs = newJobs;

                if (sourceConfig.PagesLimit.HasValue && pageNumber > sourceConfig.PagesLimit.Value)
                    break;

                Logger.LogInformation("{DataOrigin} - scraping page {PageNumber}", Origin, pageNumber);

                // scroll down
                var scrollHeight = pageNumber * 1200;
                await page.EvaluateAsync($"window.scrollTo(0, {scrollHeight});");
                await page.WaitForTimeoutAsync(ScrapeConfig.WaitForScrollSeconds * 1000);

                await SaveScreenshot(page, $"{Origin}/list/{fetchDate}/{pageNumber}.png");
                await SavePage(page, $"{Origin}/list/{fetchDate}/{pageNumber}.html");

                var jobsFromPage = await ScrapeJobsFromList(page);

                var previousOfferUrls = previousJobs.Select(sc => sc.OfferUrl).ToArray();
                newJobs = jobsFromPage.Where(j => !previousOfferUrls.Contains(j.OfferUrl)).ToList();

                yield return newJobs;
            }

            LogOriginKeyScrappingComplete(Logger, Origin);
        }

        private async Task AcceptCookies(IPage page)
        {
            var cookiesAccept = await page.QuerySelectorAsync("#cookiescript_accept");
            if (cookiesAccept is null)
                return;

            Logger.LogInformation("{DataOrigin} - accepting cookies", Origin);
            await cookiesAccept.ClickAsync();

            await page.WaitForTimeoutAsync(1 * 1000);
        }

        private async Task<List<JobOffer>> ScrapeJobsFromList(IPage page)
        {
            var script =
                await ScrapeHelpers.GetJsScript(
                    "JobScraper.Web.Features.JobOffers.Scrape.Logic.Jjit.jjit-list.js"); // yes, jjit, same layout
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
                    Origin = Origin,
                    DetailsScrapeStatus = DetailsScrapeStatus.ToScrape,
                };

                SalaryParser.TryParseSalary(jobOffer, data.Salary);
                jobOffer.SetDefaultDescription();

                jobs.Add(jobOffer);
            }

            Logger.LogInformation("{DataOrigin} scraping completed. Total jobs: {JobsCount}", Origin, jobs.Count);

            return jobs;
        }


        private record JobData(
            string Title,
            string Url,
            string CompanyName,
            string Location,
            string Salary,
            List<string> OfferKeywords
        );

        [LoggerMessage(LogLevel.Information, "{dataOrigin} - scrapping complete")]
        static partial void LogOriginKeyScrappingComplete(ILogger<ScraperBaseHandler> logger, string dataOrigin);
    }
}
