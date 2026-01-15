using System.Globalization;
using System.Text;
using System.Text.Json;
using JobScraper.Entities;
using JobScraper.Persistence;
using JobScraper.Web.Scraping.Common;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace JobScraper.Web.Scraping.PracujPl;

public class PracujPlListScraper
{
    public record Command(SourceConfig Source) : ScrapeCommand(Source);

    public class Handler : ListScraperBase<Command>
    {

        protected override DataOrigin DataOrigin => DataOrigin.PracujPl;
        public Handler(IOptions<AppSettings> config,
            ILogger<Handler> logger,
            JobsDbContext dbContext)
            : base(config, logger, dbContext)
        { }

        public override async IAsyncEnumerable<List<JobOffer>> ScrapeJobs(SourceConfig sourceConfig)
        {
            var searchUrl = sourceConfig.SearchUrl;
            Logger.LogInformation("{DataOrigin} scraping for url {SearchUrl}", DataOrigin, searchUrl);

            var page = await LoadUntilAsync(searchUrl, waitSeconds: ScrapeConfig.WaitForListSeconds);

            var fetchDate = DateTime.UtcNow.ToString("yyMMdd_HHmm");
            var pageNumber = 0;

            await SaveScreenshot(page, $"{DataOrigin}/list/{fetchDate}/{pageNumber}.png");
            await SavePage(page, $"{DataOrigin}/list/{fetchDate}/{pageNumber}.html");

            var cookieButton = await page.QuerySelectorAsync("button[data-test='button-submitCookie']");
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

                var nextButton = await page.QuerySelectorAsync("button[data-test='bottom-pagination-button-next']");
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
                var url = data.OfferUrls.FirstOrDefault()?.Split('?').FirstOrDefault();
                if (string.IsNullOrEmpty(url))
                    continue;

                var description = ParseDescription(data);
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
                    PublishedAt = ParseDate(data.PublishDate),
                    DetailsScrapeStatus = DetailsScrapeStatus.Scraped, // skip details scraping
                };

                SalaryParser.TryParseSalary(jobOffer, data.Salary);

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
                descBuilder.AppendLine(offerUrl.Split('?').FirstOrDefault());

            return descBuilder.ToString();
        }

        internal static DateTime? ParseDate(string dateStr) =>
            DateTime.TryParse(dateStr, new CultureInfo("pl-PL"), out var dateTime)
                ? dateTime
                : null;

        private record JobData(
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
    }
}
