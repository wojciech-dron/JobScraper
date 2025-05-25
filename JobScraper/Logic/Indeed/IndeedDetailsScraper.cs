using System.Text.RegularExpressions;
using JobScraper.Logic.Common;
using JobScraper.Models;
using JobScraper.Persistence;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace JobScraper.Logic.Indeed;

public class IndeedDetailsScraper
{
    public record Command : ScrapeCommand;

    public class Handler : DetailsScrapperBase<Command>
    {
        public Handler(IOptions<ScraperConfig> config,
            ILogger<Handler> logger,
            JobsDbContext dbContext)
            : base(config, logger, dbContext)
        { }

        protected override DataOrigin DataOrigin => DataOrigin.Indeed;

        public override async Task<JobOffer> ScrapeJobDetails(JobOffer jobOffer)
        {
            Logger.LogInformation("Scraping job details for {OfferUrl}", jobOffer.OfferUrl);

            var indeedPage = await LoadUntilAsync(jobOffer.OfferUrl, waitSeconds: ScrapeConfig.WaitForDetailsSeconds);
            await indeedPage.WaitForTimeoutAsync(ScrapeConfig.WaitForDetailsSeconds * 1000); // Wait for the page to load

            jobOffer.ScreenShotPath = $"indeed/{jobOffer.CompanyName}/{DateTime.UtcNow:yyMMdd_HHmm}.png";
            await SaveScreenshot(indeedPage, jobOffer.ScreenShotPath);

            jobOffer.HtmlPath = $"indeed/{jobOffer.CompanyName}/{DateTime.UtcNow:yyMMdd_HHmm}.html";
            await SavePage(indeedPage, jobOffer.HtmlPath);

            jobOffer.PublishedAt = DateTime.UtcNow;

            await Task.WhenAll(
                ScrapApplyUrl(jobOffer, indeedPage),
                ScrapDescription(jobOffer, indeedPage),
                ScrapCompany(jobOffer.Company, indeedPage),
                ScrapSalary(jobOffer, indeedPage)
            );

            return jobOffer;
        }

        private static async Task ScrapSalary(JobOffer job, IPage page)
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
        private async Task ScrapDescription(JobOffer jobOffer, IPage page)
        {
            var jobDescription = await page.EvaluateAsync<string>(
                "document.querySelector('#jobDescriptionText')?.innerText ?? ''");

            if (string.IsNullOrWhiteSpace(jobDescription))
                return;

            jobOffer.Description = jobDescription;
            jobOffer.MyKeywords = FindMyKeywords(jobOffer);
        }


        private static async Task ScrapApplyUrl(JobOffer jobOffer, IPage page)
        {
            var indeedApplyElement = await page.QuerySelectorAsync(
                "span[data-indeed-apply-joburl], button[href*='https://www.indeed.com/applystart?jk=']");
            if (indeedApplyElement != null)
            {
                jobOffer.ApplyUrl = await indeedApplyElement.GetAttributeAsync("data-indeed-apply-joburl");
            }

            var externalApplyElement = await page.QuerySelectorAsync("button[aria-haspopup='dialog']");
            if (externalApplyElement is not null)
            {
                jobOffer.ApplyUrl = await externalApplyElement.GetAttributeAsync("href");
            }
        }

        private async Task ScrapCompany(Company company, IPage page)
        {
            var url = await page.EvaluateAsync<string?>(
                "document.querySelector('div[data-company-name] > span > a')?.getAttribute('href')");

            company.IndeedUrl = url;
        }
    }
}

