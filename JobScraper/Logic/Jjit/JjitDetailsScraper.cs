using JobScraper.Logic.Common;
using JobScraper.Models;
using JobScraper.Persistence;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace JobScraper.Logic.Jjit;

public class JjitDetailsScraper
{
    public record Command : ScrapeCommand;

    public class Handler : DetailsScrapperBase<Command>
    {
        public Handler(IOptions<AppSettings> config,
            ILogger<Handler> logger,
            JobsDbContext dbContext)
            : base(config, logger, dbContext)
        { }

        protected override DataOrigin DataOrigin => DataOrigin.JustJoinIt;

        public override async Task<JobOffer> ScrapeJobDetails(JobOffer jobOffer)
        {
            Logger.LogInformation("Scraping job details for {OfferUrl}", jobOffer.OfferUrl);

            var page = await LoadUntilAsync(jobOffer.OfferUrl, waitSeconds: ScrapeConfig.WaitForDetailsSeconds);
            await page.WaitForTimeoutAsync(ScrapeConfig.WaitForDetailsSeconds * 1000); // Wait for the page to load

            jobOffer.ScreenShotPath = $"jjit/{jobOffer.CompanyName}/{DateTime.UtcNow:yyMMdd_HHmm}.png";
            await SaveScreenshot(page, jobOffer.ScreenShotPath);

            jobOffer.HtmlPath = $"jjit/{jobOffer.CompanyName}/{DateTime.UtcNow:yyMMdd_HHmm}.html";
            await SavePage(page, jobOffer.HtmlPath);

            await Task.WhenAll(
                ScrapeFromInjectScript(jobOffer, page),
                ScrapDescription(jobOffer, page),
                ScrapCompany(jobOffer.Company!, page)
            );

            return jobOffer;
        }

        private async Task ScrapDescription(JobOffer jobOffer, IPage page)
        {
            var description = await page.EvaluateAsync<string?>(@"() => {
            let h3Elements = document.querySelectorAll('h3.MuiTypography-root.MuiTypography-h3')
            return h3Elements[h3Elements.length - 1]?.parentElement?.parentElement?.textContent
        }");

            jobOffer.Description = description;

            if (description is null)
                return;

            jobOffer.MyKeywords = ScrapeConfig.MyKeywords
                .Where(keyword => jobOffer.Description!.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        private static async Task ScrapeFromInjectScript(JobOffer job, IPage page)
        {
            var dataInjectScript = await page.EvaluateAsync<string?>(@"
            Array.from(document.querySelectorAll('script')).findLast(s => s.textContent.includes('createdAt'))?.textContent
        ");

            var dataLines = dataInjectScript?.Split(',');

            job.PublishedAt = Extract(dataLines, "publishedAt").TryParseDate();
            job.ApplyUrl = Extract(dataLines, "applyUrl");
        }

        private static string? Extract(string[]? dataLines, string key)
        {
            var applyUrl = dataLines?.FirstOrDefault(l => l.Contains(key))?.Split("\"")[^2].Replace("\\", "");

            if (applyUrl?.StartsWith("https") == true)
                return applyUrl;

            return null;
        }

        private async Task ScrapCompany(Company company, IPage page)
        {
            var url = await page.EvaluateAsync<string?>(
                "document.querySelector('a[name=\\\"company_profile_button\\\"]')?.getAttribute('href')");

            company.JjitUrl = url;
        }
    }
}