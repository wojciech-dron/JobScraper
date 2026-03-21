using System.Text.Json;
using JobScraper.Web.Common.Entities;
using JobScraper.Web.Features.JobOffers.Scrape.Logic.Common;
using JobScraper.Web.Modules.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JobScraper.Web.Features.JobOffers.Scrape.Logic.Custom;

public class CustomDetailsScraper
{
    public record Command(SourceConfig Source) : ScrapeDetailsCommand(Source);

    public sealed class Handler(
        IOptions<AppSettings> config,
        ILogger<Handler> logger,
        JobsDbContext dbContext)
        : DetailsScraperBaseHandler<Command>(config, logger, dbContext)
    {
        public override async Task<JobOffer> ScrapeJobDetails(JobOffer jobOffer)
        {
            var config = await DbContext.Set<CustomScraperConfig>()
                .FirstOrDefaultAsync(x => x.DataOrigin == jobOffer.Origin);

            if (config is null || !config.DetailsScrapingEnabled || string.IsNullOrEmpty(config.DetailsScraperScript))
                return jobOffer;

            var page = await LoadUntilAsync(jobOffer.OfferUrl, waitSeconds: ScrapeConfig.WaitForDetailsSeconds);

            var result = await page.EvaluateAsync<string>(config.DetailsScraperScript);
            var data = JsonSerializer.Deserialize<DetailsData>(result, JsonOptions);

            if (data is null)
                return jobOffer;

            if (!string.IsNullOrEmpty(data.Description))
                jobOffer.Description = data.Description;

            if (data.Keywords?.Count > 0)
                jobOffer.OfferKeywords = data.Keywords;

            return jobOffer;
        }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        private record DetailsData(string? Description, List<string>? Keywords);
    }
}
