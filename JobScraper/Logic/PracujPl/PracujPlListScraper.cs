using System.Globalization;
using System.Text;
using System.Text.Json;
using JobScraper.Logic.Common;
using JobScraper.Models;
using JobScraper.Persistence;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace JobScraper.Logic.PracujPl;

public class PracujPlListScraper
{
    public record Command : ScrapeCommand;

    public class Handler : ListScraperBase<Command>
    {
        public Handler(IOptions<ScraperConfig> config,
            ILogger<Handler> logger,
            JobsDbContext dbContext)
            : base(config, logger, dbContext)
        { }

        protected override DataOrigin DataOrigin => DataOrigin.PracujPl;

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

            // scrape first page
            var newJobs = await ScrapeJobsFromList(page);
            var previousJobs = new List<JobOffer>();
            yield return newJobs;

            while (newJobs.Count > 0)
            {
                pageNumber++;
                previousJobs.AddRange(newJobs);

                Logger.LogInformation("{DataOrigin} - scraping page {PageNumber}", DataOrigin, pageNumber);

                var nextButton = await page.QuerySelectorAsync("button.tw-btn.tw-btn-primary.tw-px-8.tw-block.tw-btn-xl");

                if (nextButton is null)
                    break;

                await nextButton.ClickAsync();
                await page.WaitForTimeoutAsync(ScrapeConfig.WaitForListSeconds * 1000);

                await SaveScreenshot(page, $"{DataOrigin}/list/{fetchDate}/{pageNumber}.png");
                await SavePage(page, $"{DataOrigin}/list/{fetchDate}/{pageNumber}.html");

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
            string[] OfferUrls,
            string Salary,
            string[] JobKeys,
            string Description,
            string CompanyName,
            string CompanyUrl,
            string Location,
            string PublishDate
        );

        private async Task<List<JobOffer>> ScrapeJobsFromList(IPage page)
        {
            var result = await page.EvaluateAsync<string>(
                """
                () => {
                    // expand details
                    const extendDetailsButtons = document.querySelectorAll('[data-test="section-short-description"] div.invisible span');
                    [...extendDetailsButtons].forEach(x => x.click());
                
                    const offersContainers = document.querySelectorAll('div[data-test="positioned-offer"], div[data-test="default-offer"]');
                    const results = Array.from(offersContainers).map(offer => {        
                        const offerUrlElements = offer.querySelectorAll('a[data-test="link-offer"]');
                        const jobKeysElements = offer.querySelectorAll('ul li');
                        const descElements = document.querySelectorAll('li[data-test^="offer-additional-info-"]');
                
                        const data = {        
                            Title: offer.querySelector('h2[data-test="offer-title"]')?.textContent.trim() ?? '',
                            OfferUrls: Array.from(offerUrlElements).map(item => item.href.trim()),
                            Salary: offer.querySelector('span[data-test="offer-salary"]')?.textContent.trim() ?? '',
                            JobKeys: Array.from(jobKeysElements).map(item => item.textContent.trim()),
                            Description: offer.querySelector('[data-test="section-short-description"]')?.textContent.trim() ?? '',
                            CompanyName: offer.querySelector('[data-test="text-company-name"]')?.textContent.trim() ?? '',
                            CompanyUrl: offer.querySelector('[data-test="link-company-profile"]')?.href.trim() ?? '',
                            Location: offer.querySelector('h4[data-test="text-region"]')?.textContent.trim() ?? '',
                            PublishDate: offer.querySelector('p[data-test="text-added"]')?.textContent.trim().split(': ')[1] ?? ''
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
                var url = data.OfferUrls.FirstOrDefault();
                if (string.IsNullOrEmpty(url))
                    continue;

                var description = ParseDescription(data);
                var myKeywords = FindMyKeywords(description);
                var dataLocation = data.Location.Replace("Miejsce pracy:", "").Replace("Siedziba firmy:", "");

                var jobOffer = new JobOffer
                {
                    Title = data.Title,
                    OfferUrl = url,
                    OfferKeywords = data.JobKeys.ToList(),
                    CompanyName = data.CompanyName,
                    Location = dataLocation,
                    Origin = DataOrigin,
                    Description = description,
                    MyKeywords = myKeywords,
                    PublishedAt = ParseDate(data.PublishDate),
                    DetailsScrapeStatus = DetailsScrapeStatus.Scraped, // skip details scraping
                };

                SalaryParser.SetSalary(jobOffer, data.Salary);

                jobs.Add(jobOffer);
            }

            return jobs;
        }

        private static string ParseDescription(JobData data)
        {
            var descBuilder = new StringBuilder(data.Description.Replace("O projekcie", ""));
            descBuilder.AppendLine();

            foreach (var desc in data.JobKeys)
                descBuilder.AppendLine(desc);

            if (data.OfferUrls.Length <= 1)
                return descBuilder.ToString();

            descBuilder.AppendLine();
            descBuilder.AppendLine("Offer has more links:");

            foreach (var offerUrl in data.OfferUrls)
                descBuilder.AppendLine(offerUrl);

            return descBuilder.ToString();
        }

        internal static DateTime? ParseDate(string dateStr) =>
            DateTime.TryParse(dateStr, new CultureInfo("pl-PL"), out var dateTime)
                ? dateTime
                : null;
    }
}
