using System.Text.Json;
using JobScraper.Logic.Common;
using JobScraper.Models;
using JobScraper.Persistence;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace JobScraper.Logic.PracujPl;

[Obsolete("Not used, data from list is enough")]
public class PracujPlDetailsScraper
{
    public record Command : ScrapeCommand;

    public class Handler : DetailsScrapperBase<Command>
    {
        public Handler(IOptions<ScraperConfig> config,
            ILogger<Handler> logger,
            JobsDbContext dbContext)
            : base(config, logger, dbContext)
        { }

        protected override DataOrigin DataOrigin => DataOrigin.PracujPl;

        public override async Task<JobOffer> ScrapeJobDetails(JobOffer jobOffer)
        {
            Logger.LogInformation("Scraping job details for {OfferUrl}", jobOffer.OfferUrl);
            var page = await LoadUntilAsync(jobOffer.OfferUrl, waitSeconds: ScrapeConfig.WaitForDetailsSeconds);

            await SaveScreenshot(jobOffer, page);
            await SavePage(jobOffer, page);
            await ScrapeDetails(jobOffer, page);

            return jobOffer;
        }

        record JobData(string? Description);

        private async Task ScrapeDetails(JobOffer jobOffer, IPage page)
        {
            var result = await page.EvaluateAsync<string>(
                """
                () => {
                    const results = {
                        description: document.querySelector('#offer-details div')?.textContent
                    };
                    console.log(results);
                    
                    return JSON.stringify(results);
                };
                """);

            var data = JsonSerializer.Deserialize<JobData>(result)!;

            if (data.Description is null)
                return;

            jobOffer.Description += data.Description;
        }
    }
}