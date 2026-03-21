using System.Text.Json;
using ErrorOr;
using JobScraper.Web.Common.Entities;
using JobScraper.Web.Common.Models;
using JobScraper.Web.Features.CustomScrapers.Models;
using JobScraper.Web.Features.JobOffers.Scrape.Logic.Common;
using JobScraper.Web.Modules.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JobScraper.Web.Features.CustomScrapers.Logic;

public partial class TestFullScenario
{
    public record Command(
        string ListUrl,
        string ListScript,
        string PaginationScript,
        string DetailsScript
    ) : IRequest<ErrorOr<TestCustomScript.Response>>;

    public sealed partial class Handler(
        IOptions<AppSettings> appSettings,
        JobsDbContext dbContext,
        ILogger<Handler> logger) : IRequestHandler<Command, ErrorOr<TestCustomScript.Response>>, IAsyncDisposable
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
        };

        private DefaultPageFactory? _pageFactory;

        [LoggerMessage(LogLevel.Information, "Testing full scenario against {ListUrl}")]
        private static partial void LogTesting(ILogger logger, string listUrl);

        [LoggerMessage(LogLevel.Error, "Full scenario test execution failed")]
        private static partial void LogTestFailed(ILogger logger, Exception exception);

        public async ValueTask<ErrorOr<TestCustomScript.Response>> Handle(Command command, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(command.ListUrl) || string.IsNullOrWhiteSpace(command.ListScript))
                return Error.Validation("ListUrl and ListScript are required");

            LogTesting(logger, command.ListUrl);

            try
            {
                var scrapeConfig = await dbContext.ScraperConfigs.AsNoTracking().FirstAsync(ct);

                _pageFactory = new DefaultPageFactory
                {
                    AppSettings = appSettings.Value,
                    ScrapeConfig = scrapeConfig,
                };

                var page = await _pageFactory.NewPageAsync();
                await page.GotoAsync(command.ListUrl, new() { Timeout = 30_000 });
                await page.WaitForTimeoutAsync(3000);

                var targetPage = 1;

                if (!string.IsNullOrWhiteSpace(command.PaginationScript))
                {
                    var paginationRaw = await page.EvaluateAsync<string>(
                        $"(pageNumber) => {{ const fn = {command.PaginationScript}; return fn(pageNumber); }}", 1);
                    var pagination = JsonSerializer.Deserialize<CustomPaginationResult>(paginationRaw, JsonOptions);

                    if (pagination?.HasNextPage == true)
                    {
                        targetPage = 2;
                        if (!string.IsNullOrEmpty(pagination.NextPageUrl))
                            await page.GotoAsync(pagination.NextPageUrl, new() { Timeout = 30_000 });
                        await page.WaitForTimeoutAsync(3000);
                    }
                }

                var listRaw = await page.EvaluateAsync<string>(command.ListScript);
                var items = JsonSerializer.Deserialize<CustomJobData[]>(listRaw, JsonOptions) ?? [];

                if (items.Length == 0)
                    return Error.Failure(description: $"List script returned no offers on page {targetPage}");

                var firstOffer = items[0];

                var salaryMin = firstOffer.SalaryMinMonth;
                var salaryMax = firstOffer.SalaryMaxMonth;
                var currency = firstOffer.SalaryCurrency;
                var salaryParsed = salaryMin.HasValue;

                if (!salaryParsed && !string.IsNullOrEmpty(firstOffer.SalaryToParse))
                    salaryParsed = SalaryParser.TryParseSalary(firstOffer.SalaryToParse, out salaryMin, out salaryMax, out currency);

                var detailsResult = (object?)null;

                if (!string.IsNullOrWhiteSpace(command.DetailsScript) && !string.IsNullOrEmpty(firstOffer.Url))
                {
                    await page.GotoAsync(firstOffer.Url, new() { Timeout = 30_000 });
                    await page.WaitForTimeoutAsync(3000);
                    var detailsRaw = await page.EvaluateAsync<string>(command.DetailsScript);
                    detailsResult = JsonSerializer.Deserialize<CustomDetailsData>(detailsRaw, JsonOptions);
                }

                var fullResult = new
                {
                    Page = targetPage,
                    FirstOffer = new
                    {
                        firstOffer.Title,
                        firstOffer.Url,
                        firstOffer.CompanyName,
                        firstOffer.Location,
                        firstOffer.Description,
                        firstOffer.OfferKeywords,
                        firstOffer.SalaryToParse,
                        SalaryParsed = salaryParsed,
                        SalaryMinMonth = salaryMin ?? firstOffer.SalaryMinMonth,
                        SalaryMaxMonth = salaryMax ?? firstOffer.SalaryMaxMonth,
                        SalaryCurrency = currency ?? firstOffer.SalaryCurrency,
                    },
                    Details = detailsResult,
                };

                return new TestCustomScript.Response(JsonSerializer.Serialize(fullResult, JsonOptions));
            }
            catch (Exception ex)
            {
                LogTestFailed(logger, ex);
                return Error.Failure(description: ex.Message);
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_pageFactory is not null)
                await _pageFactory.DisposeAsync();
        }
    }
}
