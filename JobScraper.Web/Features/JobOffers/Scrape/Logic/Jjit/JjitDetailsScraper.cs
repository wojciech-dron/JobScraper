using System.Text.Json;
using JobScraper.Web.Common.Entities;
using JobScraper.Web.Features.JobOffers.Scrape.Logic.Common;
using JobScraper.Web.Modules.Persistence;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace JobScraper.Web.Features.JobOffers.Scrape.Logic.Jjit;

public class JjitDetailsScraper
{
    public record Command(SourceConfig Source) : ScrapeCommand(Source);

    public class Handler : DetailsScrapperBase<Command>
    {

        protected override DataOrigin DataOrigin => DataOrigin.JustJoinIt;
        public Handler(IOptions<AppSettings> config,
            ILogger<Handler> logger,
            JobsDbContext dbContext)
            : base(config, logger, dbContext)
        { }

        public override async Task<JobOffer> ScrapeJobDetails(JobOffer jobOffer)
        {
            Logger.LogInformation("Scraping {DataOrigin} job details for {OfferUrl}", DataOrigin, jobOffer.OfferUrl);

            var page = await LoadUntilAsync(jobOffer.OfferUrl, waitSeconds: ScrapeConfig.WaitForDetailsSeconds);
            await page.WaitForTimeoutAsync(ScrapeConfig.WaitForDetailsSeconds * 1000); // Wait for the page to load

            await SaveScreenshot(jobOffer, page);
            await SavePage(jobOffer, page);

            await ScrapeDescription(jobOffer, page);

            return jobOffer;
        }

        private async Task ScrapeDescription(JobOffer jobOffer, IPage page)
        {
            var result = await page.EvaluateAsync<string>(
                """
                () => {
                    let descDiv = document.querySelector('h3').parentNode
                    const result = {
                        Description: [...descDiv.childNodes].slice(1).map(x => x?.textContent).join(`\n`),
                        Keywords: [...descDiv.querySelectorAll('h4')].map(x => x?.textContent?.trim()),
                        CompanyUrl: document.querySelector('svg[data-testid="ApartmentRoundedIcon"]')?.parentNode.parentNode.getAttribute('href')
                    }
                    console.log(result)

                    return JSON.stringify(result)
                }
                """);

            var data = JsonSerializer.Deserialize<JobData>(result)!;

            jobOffer.Description = data.Description;
            jobOffer.OfferKeywords.AddRange(data.Keywords);

            if (!string.IsNullOrEmpty(data.CompanyUrl))
                jobOffer.Company!.JjitUrl = BaseUrl + data.CompanyUrl;
        }

        private record JobData(string Description, string CompanyUrl, List<string> Keywords);
    }
}
