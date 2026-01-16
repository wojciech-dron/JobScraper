using System.Text;
using System.Text.Json;
using JobScraper.Web.Common.Entities;
using JobScraper.Web.Features.Scrape.Logic.Common;
using JobScraper.Web.Modules.Persistence;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace JobScraper.Web.Features.Scrape.Logic.Olx;

public class OlxListScraper
{
    public record Command(SourceConfig Source) : ScrapeCommand(Source);

    public class Handler : ListScraperBase<Command>
    {

        protected override DataOrigin DataOrigin => DataOrigin.Olx;
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

            var cookieButton = await page.QuerySelectorAsync("#onetrust-accept-btn-handler");
            if (cookieButton is not null)
                await cookieButton.ClickAsync();

            // scrape first page
            var newJobs = await ScrapeJobsFromList(page);
            var previousJobs = new List<JobOffer>();
            yield return newJobs;

            while (newJobs.Count > 0)
            {
                pageNumber++;
                previousJobs.AddRange(newJobs);

                Logger.LogInformation("{DataOrigin} - scraping page {PageNumber}", DataOrigin, pageNumber);

                var nextButton = await page.QuerySelectorAsync("a[data-testid='pagination-forward']");
                if (nextButton is null)
                    break;

                await nextButton.ClickAsync();
                await page.WaitForTimeoutAsync(ScrapeConfig.WaitForListSeconds * 1000);

                // await SaveScreenshot(page, $"{DataOrigin}/list/{fetchDate}/{pageNumber}.png");
                // await SavePage(page, $"{DataOrigin}/list/{fetchDate}/{pageNumber}.html");

                // scrape next page
                var jobsFromPage = await ScrapeJobsFromList(page);

                var previousOfferUrls = previousJobs.Select(sc => sc.OfferUrl);
                newJobs = jobsFromPage.Where(j => !previousOfferUrls.Contains(j.OfferUrl)).ToList();

                yield return newJobs;
            }

            Logger.LogInformation("{DataOrigin} - scrapping complete", DataOrigin);
        }

        private async Task<List<JobOffer>> ScrapeJobsFromList(IPage page)
        {
            var result = await page.EvaluateAsync<string>(
                """
                () => {
                    const offersContainers = document.querySelectorAll('div.jobs-ad-card');

                    const results = Array.from(offersContainers).map(offer => {
                        const titleContainer = [...offer.querySelectorAll('div > div > a')].at(-1);
                        const firstRow = offer.querySelector('div:nth-child(2) > div > div');
                        const secondRow = offer.querySelector('div:nth-child(2) > div > div:nth-child(2)');

                        const data = {
                            Title: titleContainer?.textContent,
                            Url: titleContainer?.getAttribute('href')?.trim(),
                            CompanyName: titleContainer.parentNode.querySelector('p')?.textContent?.trim(),
                            FirstRowData: [...firstRow.querySelectorAll('div > div > p')].map(p => p.textContent?.trim()).reverse(),
                            SecondRowData: [...secondRow.querySelectorAll('button')].map(p => p.textContent?.trim()),
                        };

                        return data;
                    });
                    console.log(results);
                return JSON.stringify(results);
                };
                """);

            var scrapedOffers = JsonSerializer.Deserialize<JobData[]>(result)!;

            var jobs = new List<JobOffer>();
            foreach (var data in scrapedOffers)
            {
                if (string.IsNullOrEmpty(data.Url))
                    continue;

                if (jobs.Any(j => j.OfferUrl.EndsWith(data.Url)))
                    continue;

                var jobOffer = new JobOffer
                {
                    Title = data.Title,
                    OfferUrl = BaseUrl + data.Url,
                    CompanyName = data.CompanyName,
                    OfferKeywords = data.SecondRowData,
                    Origin = DataOrigin,
                    DetailsScrapeStatus = DetailsScrapeStatus.Scraped, // skip details scraping
                };

                // skip first row if salary parsed successfully
                if (SalaryParser.TryParseSalary(jobOffer, data.FirstRowData.First()))
                    data.FirstRowData.Pop();

                jobOffer.Location = data.FirstRowData.Pop();
                jobOffer.OfferKeywords.AddRange(data.FirstRowData);
                jobOffer.Description = GetDescription(jobOffer);

                jobs.Add(jobOffer);
            }

            return jobs;
        }

        private static string GetDescription(JobOffer jobOffer)
        {
            var stringBuilder = new StringBuilder();

            foreach (var keyword in jobOffer.OfferKeywords)
                stringBuilder.AppendLine(keyword);

            return stringBuilder.ToString();
        }

        private record JobData(
            string Title,
            string Url,
            string? CompanyName,
            Stack<string> FirstRowData,
            List<string> SecondRowData
        );
    }
}
