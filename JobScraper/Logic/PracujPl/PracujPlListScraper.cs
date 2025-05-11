using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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

            await SaveScrenshoot(page, $"{DataOrigin}/list/{fetchDate}/{pageNumber}.png");
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

                await SaveScrenshoot(page, $"{DataOrigin}/list/{fetchDate}/{pageNumber}.png");
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
                    const offersContainers = document.querySelectorAll('div[data-test="positioned-offer"], div[data-test="default-offer"]');

                    const results = Array.from(offersContainers).map(offer => {        
                      const offerUrlElements = offer.querySelectorAll('a[data-test="link-offer"]');
                      const jobKeysElements = offer.querySelectorAll('ul li');

                      return {        
                          Title: offer.querySelector('h2[data-test="offer-title"]')?.textContent.trim() ?? '',
                          OfferUrls: Array.from(offerUrlElements).map(item => item.href.trim()),
                          Salary: offer.querySelector('span[data-test="offer-salary"]')?.textContent.trim() ?? '',
                          JobKeys: Array.from(jobKeysElements).map(item => item.textContent.trim()),
                          CompanyName: offer.querySelector('[data-test="text-company-name"]')?.textContent.trim() ?? '',
                          CompanyUrl: offer.querySelector('[data-test="link-company-profile"]')?.href.trim() ?? '',
                          Location: offer.querySelector('h4[data-test="text-region"]')?.textContent.trim() ?? '',
                          PublishDate: offer.querySelector('p[data-test="text-added"]')?.textContent.trim().split(': ')[1] ?? ''
                      };
                    });

                    return JSON.stringify(results);
                }
                """);

            var scrapedOffers = JsonSerializer.Deserialize<JobData[]>(result)!;

            var jobs = new List<JobOffer>();
            foreach (var data in scrapedOffers)
            {
                var url = data.OfferUrls.FirstOrDefault();
                if (string.IsNullOrEmpty(url))
                    continue;

                var description = new StringBuilder();
                if (data.OfferUrls.Length > 1)
                {
                    description.AppendLine("Offer has more links:");
                    foreach (var offerUrl in data.OfferUrls)
                    {
                        description.AppendLine(offerUrl);
                    }
                }

                var jobOffer = new JobOffer
                {
                    Title = data.Title,
                    OfferUrl = url,
                    OfferKeywords = data.JobKeys.ToList(),
                    CompanyName = data.CompanyName,
                    Location = data.Location,
                    Origin = DataOrigin,
                    Description = description.ToString()
                };

                SetSalary(jobOffer, data.Salary);

                jobs.Add(jobOffer);
            }

            return jobs;
        }

        private void SetSalary(JobOffer jobOffer, string salary)
        {
            // Example: "6 500–7 500 zł brutto / mies."
            // Example: "200–220 zł netto (+ VAT) / godz."
            var match = Regex.Match(salary, @"^(\d+)–(\d+)([A-Z]+)$");

            if (!match.Success)
                return;

            jobOffer.SalaryMinMonth = int.Parse(match.Groups[1].Value);
            jobOffer.SalaryMaxMonth = int.Parse(match.Groups[2].Value);
            jobOffer.SalaryCurrency = match.Groups[3].Value;
        }
    }
}
