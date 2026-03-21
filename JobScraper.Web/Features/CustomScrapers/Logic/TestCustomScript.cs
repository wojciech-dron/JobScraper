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
using Microsoft.Playwright;

namespace JobScraper.Web.Features.CustomScrapers.Logic;

public partial class TestCustomScript
{
    public record Command(string TestUrl, string Script, ScriptType Type) : IRequest<ErrorOr<Response>>;

    public record Response(string RawResult);

    public enum ScriptType
    {
        List,
        Details,
        Pagination,
    }

    public sealed partial class Handler(
        IOptions<AppSettings> appSettings,
        JobsDbContext dbContext,
        ILogger<Handler> logger) : IRequestHandler<Command, ErrorOr<Response>>, IAsyncDisposable
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
        };

        private DefaultPageFactory? _pageFactory;

        [LoggerMessage(LogLevel.Information, "Testing {ScriptType} script against {TestUrl}")]
        private static partial void LogTesting(ILogger logger, ScriptType scriptType, string testUrl);

        [LoggerMessage(LogLevel.Error, "Test script execution failed")]
        private static partial void LogTestFailed(ILogger logger, Exception exception);

        public async ValueTask<ErrorOr<Response>> Handle(Command command, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(command.TestUrl) || string.IsNullOrWhiteSpace(command.Script))
                return Error.Validation("TestUrl and Script are required");

            LogTesting(logger, command.Type, command.TestUrl);

            try
            {
                var scrapeConfig = await dbContext.ScraperConfigs.AsNoTracking().FirstAsync(ct);

                _pageFactory = new DefaultPageFactory
                {
                    AppSettings = appSettings.Value,
                    ScrapeConfig = scrapeConfig,
                };

                var page = await _pageFactory.NewPageAsync();
                await page.GotoAsync(command.TestUrl,
                    new PageGotoOptions
                    {
                        Timeout = 30_000,
                    });
                await page.WaitForTimeoutAsync(3000);

                var rawResult = command.Type switch
                {
                    ScriptType.Pagination => await page.EvaluateAsync<string>(
                        $"(pageNumber) => {{ const fn = {command.Script}; return fn(pageNumber); }}",
                        1),
                    _ => await page.EvaluateAsync<string>(command.Script),
                };

                var prettyResult = command.Type switch
                {
                    ScriptType.List       => FormatListResult(rawResult),
                    ScriptType.Details    => FormatDetailsResult(rawResult),
                    ScriptType.Pagination => FormatPaginationResult(rawResult),
                    _                     => rawResult,
                };

                return new Response(prettyResult);
            }
            catch (Exception ex)
            {
                LogTestFailed(logger, ex);
                return Error.Failure(description: ex.Message);
            }
        }

        private static string FormatListResult(string rawResult)
        {
            var items = JsonSerializer.Deserialize<CustomJobData[]>(rawResult, JsonOptions) ?? [];

            var enriched = items.Select(d =>
            {
                var salaryMin = d.SalaryMinMonth;
                var salaryMax = d.SalaryMaxMonth;
                var currency = d.SalaryCurrency;
                var salaryParsed = false;

                if (!salaryMin.HasValue && !string.IsNullOrEmpty(d.SalaryToParse))
                    salaryParsed = SalaryParser.TryParseSalary(d.SalaryToParse, out salaryMin, out salaryMax, out currency);

                return new
                {
                    d.Title,
                    d.Url,
                    d.CompanyName,
                    d.Location,
                    d.Description,
                    d.OfferKeywords,
                    d.SalaryToParse,
                    SalaryMinMonth = salaryMin ?? d.SalaryMinMonth,
                    SalaryMaxMonth = salaryMax ?? d.SalaryMaxMonth,
                    SalaryCurrency = currency  ?? d.SalaryCurrency,
                    SalaryParsed = salaryParsed,
                };
            }).ToArray();

            return JsonSerializer.Serialize(enriched, JsonOptions);
        }

        private static string FormatDetailsResult(string rawResult)
        {
            var data = JsonSerializer.Deserialize<CustomDetailsData>(rawResult, JsonOptions);
            return JsonSerializer.Serialize(data, JsonOptions);
        }

        private static string FormatPaginationResult(string rawResult)
        {
            var data = JsonSerializer.Deserialize<CustomPaginationResult>(rawResult, JsonOptions);
            return JsonSerializer.Serialize(data, JsonOptions);
        }

        public async ValueTask DisposeAsync()
        {
            if (_pageFactory is not null)
                await _pageFactory.DisposeAsync();
        }
    }
}
