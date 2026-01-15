using JobScraper.Entities;
using JobScraper.Persistence;
using JobScraper.Web.Scraping.Common;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace JobScraper.Web.Scraping.Indeed;

public class IndeedListScraper
{
    public record Command(SourceConfig Source) : ScrapeCommand(Source);

    public class Handler : ListScraperBase<Command>
    {
        protected override DataOrigin DataOrigin => DataOrigin.Indeed;
        public Handler(IOptions<AppSettings> config,
            ILogger<Handler> logger,
            JobsDbContext dbContext)
            : base(config, logger, dbContext)
        { }

        public override async IAsyncEnumerable<List<JobOffer>> ScrapeJobs(SourceConfig sourceConfig)
        {
            var searchUrl = sourceConfig.SearchUrl;
            Logger.LogInformation("Indeed scraping for url {SearchUrl}", searchUrl);

            var page = await LoadUntilAsync(searchUrl, waitSeconds: ScrapeConfig.WaitForListSeconds);

            var fetchDate = DateTime.UtcNow.ToString("yyMMdd_HHmm");
            var pageNumber = 0;

            await SaveScreenshot(page, $"indeed/list/{fetchDate}/{pageNumber}.png");
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
                await page.WaitForTimeoutAsync(ScrapeConfig.WaitForListSeconds * 1000);

                await SaveScreenshot(page, $"{DataOrigin}/list/{fetchDate}/{pageNumber}.png");
                await SavePage(page, $"{DataOrigin}/list/{fetchDate}/{pageNumber}.html");
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
                var offerUrl = BaseUrl + urls[i]!.Split('&')[0].Replace("/rc/clk", "/viewjob");
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
}
