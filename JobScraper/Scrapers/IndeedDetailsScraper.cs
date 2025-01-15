using System.Text;
using System.Web;
using JobScraper.Models;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace JobScraper.Scrapers;

public class IndeedDetailsScraper : ScrapperBase
{
    public IndeedDetailsScraper(IBrowser browser,
        IOptions<ScraperConfig> config,
        ILogger<IndeedDetailsScraper> logger) : base(browser, config, logger)
    { }

    public async Task<Job> ScrapeJobDetails(Job job)
    {
        var indeedPage = await LoadUntilAsync(job.OfferUrl);

        var indeedJobDescriptionElement = await indeedPage.QuerySelectorAsync("div.jobsearch-JobComponent-description");
        if (indeedJobDescriptionElement != null)
        {
            job.Description = await indeedJobDescriptionElement.InnerTextAsync();
            job.MyKeywords = Config.Keywords
                .Where(keyword => job.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        else
        {
            await SaveScrenshoot(indeedPage, "jobs\\job-screenshot.png");
            await SavePage(indeedPage, "jobs\\job.html");
        }

        var externalApplyElement = await indeedPage.QuerySelectorAsync("button[aria-haspopup='dialog']");
        if (externalApplyElement is not null)
        {
            job.ApplyUrl = await externalApplyElement.GetAttributeAsync("href");
            return job;
        }

        var indeedApplyElement = await indeedPage.QuerySelectorAsync(
            "span[data-indeed-apply-joburl], button[href*='https://www.indeed.com/applystart?jk=']");
        if (indeedApplyElement != null)
        {
            job.ApplyUrl = await indeedApplyElement.GetAttributeAsync("data-indeed-apply-joburl");
        }

        return job;
    }

}
