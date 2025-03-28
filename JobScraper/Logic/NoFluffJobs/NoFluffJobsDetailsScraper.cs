using JobScraper.Logic.Common;
using JobScraper.Models;
using JobScraper.Persistence;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace JobScraper.Logic.NoFluffJobs;

public class NoFluffJobsDetailsScraper
{
    public record Command : ScrapeCommand;

    public class Handler : DetailsScrapperBase<Command>
    {
        public Handler(IOptions<ScraperConfig> config,
            ILogger<Handler> logger,
            JobsDbContext dbContext)
            : base(config, logger, dbContext)
        { }

        protected override DataOrigin DataOrigin => DataOrigin.NoFluffJobs;

        public override async Task<JobOffer> ScrapeJobDetails(JobOffer jobOffer)
        {
            Logger.LogInformation("Scraping job details for {OfferUrl}", jobOffer.OfferUrl);

            var page = await LoadUntilAsync(jobOffer.OfferUrl, waitSeconds: ScrapeConfig.WaitForDetailsSeconds);

            jobOffer.ScreenShotPath = $"NoFluffJobs/{jobOffer.CompanyName}/{DateTime.UtcNow:yyMMdd_HHmm}.png";
            await SaveScrenshoot(page, jobOffer.ScreenShotPath);

            jobOffer.HtmlPath = $"NoFluffJobs/{jobOffer.CompanyName}/{DateTime.UtcNow:yyMMdd_HHmm}.html";
            await SavePage(page, jobOffer.HtmlPath);

            await Task.WhenAll(
                ScrapeDescription(jobOffer, page),
                ScrapCompany(jobOffer.Company, page)
            );

            return jobOffer;
        }

        private async Task ScrapeDescription(JobOffer jobOffer, IPage page)
        {
            var description = await page.EvaluateAsync<string?>(@"
                document.querySelector('common-posting-content-wrapper')?.textContent;
            ");

            if (description is null)
                return;

            jobOffer.Description = description;
            jobOffer.MyKeywords = FindMyKeywords(jobOffer);
        }

        private async Task ScrapCompany(Company company, IPage page)
        {
            var url = await page.EvaluateAsync<string?>(
                "document.querySelector('common-posting-company-about > article > header > h2 > a')?.getAttribute('href')");

            company.NoFluffJobsUrl = url;
        }
    }
}

