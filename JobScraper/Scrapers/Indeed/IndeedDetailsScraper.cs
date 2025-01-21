using JobScraper.Models;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace JobScraper.Scrapers.Indeed;

public class IndeedDetailsScraper : ScrapperBase
{
    protected override DataOrigin DataOrigin => DataOrigin.Indeed;

    public IndeedDetailsScraper(
        IOptions<ScraperConfig> config,
        ILogger<IndeedDetailsScraper> logger) : base(config, logger)
    { }

    public async Task<JobOffer> ScrapeJobDetails(JobOffer jobOffer)
    {
        Logger.LogInformation("Scrapeing job details for {OfferUrl}", jobOffer.OfferUrl);

        var indeedPage = await LoadUntilAsync(jobOffer.OfferUrl, waitSeconds: Config.WaitForDetailsSeconds);
        await indeedPage.WaitForTimeoutAsync(Config.WaitForDetailsSeconds * 1000); // Wait for the page to load

        jobOffer.ScreenShotPath = $"indeed/{jobOffer.CompanyName}/{DateTime.Now:yyMMdd_HHmm}.png";
        await SaveScrenshoot(indeedPage, jobOffer.ScreenShotPath);

        jobOffer.HtmlPath = $"indeed/{jobOffer.CompanyName}/{DateTime.Now:yyMMdd_HHmm}.html";
        await SavePage(indeedPage, jobOffer.HtmlPath);

        await Task.WhenAll(
            ScrapApplyUrl(jobOffer, indeedPage),
            ScrapDescription(jobOffer, indeedPage),
            ScrapCompany(jobOffer.Company, indeedPage)
        );

        return jobOffer;
    }

    private async Task ScrapDescription(JobOffer jobOffer, IPage page)
    {
        var indeedJobDescriptionElement = await page.QuerySelectorAsync("div.jobsearch-JobComponent-description");
        if (indeedJobDescriptionElement != null)
        {
            jobOffer.Description = await indeedJobDescriptionElement.InnerTextAsync();
            FindMyKeywords(jobOffer);
        }
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
        var url = await page.EvaluateAsync<string>(
            "document.querySelector('div[data-company-name] > span > a').getAttribute('href')");

        company.IndeedUrl = url;
    }
}
