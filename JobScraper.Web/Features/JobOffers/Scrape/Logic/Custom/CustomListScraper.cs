using System.Text.Json;
using JobScraper.Web.Common.Entities;
using JobScraper.Web.Common.Models;
using JobScraper.Web.Features.CustomScrapers.Models;
using JobScraper.Web.Features.JobOffers.Scrape.Logic.Common;
using JobScraper.Web.Modules.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JobScraper.Web.Features.JobOffers.Scrape.Logic.Custom;

public class CustomListScraper
{
    public record Command(SourceConfig Source) : ScrapeCommand(Source);

    public sealed class Handler(
        IOptions<AppSettings> config,
        ILogger<Handler> logger,
        JobsDbContext dbContext)
        : ListScraperBaseHandler<Command>(config, logger, dbContext)
    {
        public override async IAsyncEnumerable<List<JobOffer>> ScrapeJobs(SourceConfig sourceConfig)
        {
            var config = await DbContext.Set<CustomScraperConfig>()
                .FirstOrDefaultAsync(x => x.DataOrigin == Origin);

            if (config is null)
            {
                Logger.LogWarning("No CustomScraperConfig found for origin {DataOrigin}", Origin);
                yield break;
            }

            var page = await LoadUntilAsync(sourceConfig.SearchUrl, waitSeconds: ScrapeConfig.WaitForListSeconds);
            var pageNumber = 1;

            while (true)
            {
                if (sourceConfig.PagesLimit.HasValue && pageNumber > sourceConfig.PagesLimit.Value)
                    break;

                Logger.LogInformation("{DataOrigin} custom scraper - page {PageNumber}", Origin, pageNumber);

                var result = await page.EvaluateAsync<string>(config.ListScraperScript);
                var scrapedOffers = JsonSerializer.Deserialize<CustomJobData[]>(result, JsonOptions) ?? [];

                var jobs = scrapedOffers
                    .Select(d =>
                    {
                        var job = new JobOffer
                        {
                            Title = d.Title  ?? "",
                            OfferUrl = d.Url ?? "",
                            CompanyName = d.CompanyName,
                            Location = d.Location,
                            Description = d.Description,
                            OfferKeywords = d.OfferKeywords ?? [],
                            Origin = Origin,
                            DetailsScrapeStatus = config.DetailsScrapingEnabled
                                ? DetailsScrapeStatus.ToScrape
                                : DetailsScrapeStatus.Scraped,
                        };

                        if (d.SalaryMinMonth.HasValue)
                        {
                            job.SalaryMinMonth = d.SalaryMinMonth;
                            job.SalaryMaxMonth = d.SalaryMaxMonth ?? d.SalaryMinMonth;
                            job.SalaryCurrency = d.SalaryCurrency ?? "PLN";
                        }
                        else
                            SalaryParser.TryParseSalary(job, d.SalaryToParse ?? "");

                        return job;
                    })
                    .Where(j => !string.IsNullOrEmpty(j.OfferUrl))
                    .ToList();

                yield return jobs;

                if (string.IsNullOrEmpty(config.PaginationScript))
                    break;

                var pagination = await page.EvaluateAsync<string>(
                    $"(pageNumber) => {{ const fn = {config.PaginationScript}; return fn(pageNumber); }}",
                    pageNumber);

                var paginationResult = JsonSerializer.Deserialize<CustomPaginationResult>(pagination, JsonOptions);
                if (paginationResult?.HasNextPage != true)
                    break;

                if (!string.IsNullOrEmpty(paginationResult.NextPageUrl))
                    await page.GotoAsync(paginationResult.NextPageUrl);

                await page.WaitForTimeoutAsync(ScrapeConfig.WaitForListSeconds * 1000);
                pageNumber++;
            }
        }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

    }
}
