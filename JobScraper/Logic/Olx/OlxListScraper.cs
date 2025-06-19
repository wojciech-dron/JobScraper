using System.Globalization;
using System.Text;
using System.Text.Json;
using JobScraper.Logic.Common;
using JobScraper.Models;
using JobScraper.Persistence;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace JobScraper.Logic.Olx;

public class OlxListScraper
{
    public record Command : ScrapeCommand;

    public class Handler : ListScraperBase<Command>
    {
        public Handler(IOptions<ScraperConfig> config,
            ILogger<Handler> logger,
            JobsDbContext dbContext)
            : base(config, logger, dbContext)
        { }

        protected override DataOrigin DataOrigin => DataOrigin.Olx;

        public override async IAsyncEnumerable<List<JobOffer>> ScrapeJobs()
        {
            if (string.IsNullOrEmpty(SearchUrl))
                throw new ArgumentException("SearchUrl is null or empty", nameof(SearchUrl));

            Logger.LogInformation("{DataOrigin} scraping for url {SearchUrl}", DataOrigin, SearchUrl);

            var page = await LoadUntilAsync(SearchUrl, waitSeconds: ScrapeConfig.WaitForListSeconds);

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

                var nextButton = await page.QuerySelectorAsync("button[data-cy='bottom-pagination-button-next']");
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

        record JobData(
            string Title,
            string Url,
            string? CompanyName,
            List<string> FirstRowData,
            List<string> SecondRowData
        );

        private async Task<List<JobOffer>> ScrapeJobsFromList(IPage page)
        {
            var result = await page.EvaluateAsync<string>(
                """
                () => {
                    const offersContainers = document.querySelectorAll('div.jobs-ad-card');
                    
                    const results = Array.from(offersContainers).map(offer => {        
                        const titleContainer = offer.querySelector('div > div > a');
                        const firstRow = offer.querySelector('div:nth-child(2) > div > div');
                        const secondRow = offer.querySelector('div:nth-child(2) > div > div:nth-child(2)');
                
                        const data = {        
                            Title: titleContainer?.textContent,
                            Url: titleContainer?.getAttribute('href')?.trim(),
                            CompanyName: offer.querySelector('div > div > p')?.textContent?.trim(),
                            FirstRowData: [...firstRow.querySelectorAll('div > div > p')].map(p => p.textContent?.trim()),
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
                if (SalaryParser.TryParseSalary(jobOffer, data.FirstRowData[0]))
                    data.FirstRowData.RemoveAt(0);



                jobs.Add(jobOffer);
            }

            return jobs;
        }
    }
}
