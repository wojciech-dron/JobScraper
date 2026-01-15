using System.Text.RegularExpressions;
using JobScraper.Entities;
using JobScraper.Persistence;
using JobScraper.Web.Scraping.Common;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace JobScraper.Web.Scraping.Indeed;

public class IndeedDetailsScraper
{
    public record Command : ScrapeCommand;

    public class Handler : DetailsScrapperBase<Command>
    {

        protected override DataOrigin DataOrigin => DataOrigin.Indeed;
        public Handler(IOptions<AppSettings> config,
            ILogger<Handler> logger,
            JobsDbContext dbContext)
            : base(config, logger, dbContext)
        { }

        public override async Task<JobOffer> ScrapeJobDetails(JobOffer jobOffer)
        {
            Logger.LogInformation("Scraping job details for {OfferUrl}", jobOffer.OfferUrl);

            var page = await LoadUntilAsync(jobOffer.OfferUrl, waitSeconds: ScrapeConfig.WaitForDetailsSeconds);
            await page.WaitForTimeoutAsync(ScrapeConfig.WaitForDetailsSeconds * 1000); // Wait for the page to load

            await SaveScreenshot(jobOffer, page);
            await SavePage(jobOffer, page);

            jobOffer.PublishedAt = DateTime.UtcNow;

            await Task.WhenAll(
                ScrapApplyUrl(jobOffer, page),
                ScrapDescription(jobOffer, page),
                ScrapCompany(jobOffer.Company!, page),
                ScrapeSalary(jobOffer, page)
            );

            return jobOffer;
        }

        private static async Task ScrapeSalary(JobOffer job, IPage page)
        {
            var rawSalary = await page.EvaluateAsync<string?>(@"
            Array.from(document.querySelectorAll('span[class*=""js-match-insights-provider""]'))
            .filter(span => span.textContent.includes('$'))[0]?.textContent;
        ");

            if (string.IsNullOrEmpty(rawSalary))
                return;

            rawSalary = rawSalary.Replace(",", "");

            var minMaxMatch = Regex.Match(rawSalary, @"\$(\d+) - \$(\d+)");
            if (!minMaxMatch.Success)
                return;

            var period = SalaryPeriod.Month;
            if (rawSalary.Contains("hour")) period = SalaryPeriod.Hour;
            if (rawSalary.Contains("day")) period = SalaryPeriod.Day;
            if (rawSalary.Contains("week")) period = SalaryPeriod.Week;
            if (rawSalary.Contains("year")) period = SalaryPeriod.Year;

            var minMonth = int.Parse(minMaxMatch.Groups[1].Value);
            job.SalaryMinMonth = minMonth.ApplyMonthPeriod(period);

            var maxMonth = int.Parse(minMaxMatch.Groups[2].Value);
            job.SalaryMaxMonth = maxMonth.ApplyMonthPeriod(period);

            job.SalaryCurrency = "USD";
        }

        private static async Task ScrapDescription(JobOffer jobOffer, IPage page)
        {
            var jobDescription = await page.EvaluateAsync<string>(
                "document.querySelector('#jobDescriptionText')?.innerText ?? ''");

            if (string.IsNullOrWhiteSpace(jobDescription))
                return;

            jobOffer.Description = jobDescription;
        }


        private static async Task ScrapApplyUrl(JobOffer jobOffer, IPage page)
        {
            var indeedApplyElement = await page.QuerySelectorAsync(
                "span[data-indeed-apply-joburl], button[href*='https://www.indeed.com/applystart?jk=']");
            if (indeedApplyElement != null)
                jobOffer.ApplyUrl = await indeedApplyElement.GetAttributeAsync("data-indeed-apply-joburl");

            var externalApplyElement = await page.QuerySelectorAsync("button[aria-haspopup='dialog']");
            if (externalApplyElement is not null)
                jobOffer.ApplyUrl = await externalApplyElement.GetAttributeAsync("href");
        }

        private static async Task ScrapCompany(Company company, IPage page)
        {
            var url = await page.EvaluateAsync<string?>(
                "document.querySelector('div[data-company-name] > span > a')?.getAttribute('href')");

            company.IndeedUrl = url;
        }
    }
}
