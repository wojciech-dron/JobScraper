using JobScraper.Logic.Common;
using JobScraper.Models;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace JobScraper.Logic.NoFluffJobs;

public class NoFluffJobsDetailsScraper : ScrapperBase
{
    protected override DataOrigin DataOrigin => DataOrigin.JustJoinIt;

    public NoFluffJobsDetailsScraper(
        IOptions<ScraperConfig> config,
        ILogger<NoFluffJobsDetailsScraper> logger) : base(config, logger)
    { }

    public async Task<JobOffer> ScrapeJobDetails(JobOffer jobOffer)
    {
        Logger.LogInformation("Scraping job details for {OfferUrl}", jobOffer.OfferUrl);

        var page = await LoadUntilAsync(jobOffer.OfferUrl, waitSeconds: Config.WaitForDetailsSeconds);

        jobOffer.ScreenShotPath = $"NoFluffJobs/{jobOffer.CompanyName}/{DateTime.UtcNow:yyMMdd_HHmm}.png";
        await SaveScrenshoot(page, jobOffer.ScreenShotPath);

        jobOffer.HtmlPath = $"NoFluffJobs/{jobOffer.CompanyName}/{DateTime.UtcNow:yyMMdd_HHmm}.html";
        await SavePage(page, jobOffer.HtmlPath);

        await Task.WhenAll(
            ScrapDescription(jobOffer, page),
            ScrapCompany(jobOffer.Company, page)
        );

        return jobOffer;
    }

    private async Task ScrapDescription(JobOffer jobOffer, IPage page)
    {
        var description = await page.EvaluateAsync<string?>(@"
            document.querySelector('common-posting-content-wrapper').textContent;
        ");

        jobOffer.Description = description;

        if (description is null)
            return;

        jobOffer.MyKeywords = Config.Keywords
            .Where(keyword => jobOffer.Description!.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private async Task ScrapCompany(Company company, IPage page)
    {
        var url = await page.EvaluateAsync<string?>(
            "document.querySelector('common-posting-company-about > article > header > h2 > a').getAttribute('href')");

        company.NoFluffJobsUrl = url;
    }
}
