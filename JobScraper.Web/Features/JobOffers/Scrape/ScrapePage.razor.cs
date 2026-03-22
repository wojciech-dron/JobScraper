using BlazorBootstrap;
using Blazored.FluentValidation;
using JobScraper.Web.Common.Entities;
using JobScraper.Web.Common.Models;
using JobScraper.Web.Features.AiSummary.Logic;
using JobScraper.Web.Features.JobOffers.Scrape.Logic.Functions;
using JobScraper.Web.Modules.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using TickerQ.Utilities;
using TickerQ.Utilities.Entities;

namespace JobScraper.Web.Features.JobOffers.Scrape;

public partial class ScrapePage(
    IDbContextFactory<JobsDbContext> dbFactory,
    IServiceProvider serviceProvider,
    IJSRuntime js,
    IOptions<AppSettings> appSettings,
    ILogger<ScrapePage> logger)
{
    private readonly AppSettings _appSettings = appSettings.Value;
    private ScraperConfig config = new();
    private JobsDbContext dbContext = null!;
    private readonly CancellationTokenSource cts = new();

    private bool isWorking;
    private bool dashboardEnabled;
    private string[] availableOrigins = [];
    private CronTickerEntity? scrapeJobTicker;
    private FluentValidationValidator validator = null!;
    private byte[] requestBytes = [];

    protected override async Task OnInitializedAsync()
    {
        dbContext = await dbFactory.CreateDbContextAsync();

        if (string.IsNullOrEmpty(dbContext.CurrentUserName))
            throw new InvalidOperationException("User name is not set.");

        config = await dbContext.ScraperConfigs.FirstOrDefaultAsync() ?? new ScraperConfig();
        dashboardEnabled = _appSettings.TickerQ?.Dashboard?.Enabled == true;

        availableOrigins = await dbContext.CustomScraperConfigs
            .Select(x => x.DataOrigin)
            .Concat(DataOriginHelpers.Scrapable)
            .Distinct()
            .ToArrayAsync();

        requestBytes = TickerHelper.CreateTickerRequest(
            new ScrapeRequest(dbContext.CurrentUserName));

        scrapeJobTicker = await dbContext.Set<CronTickerEntity>()
            .AsNoTracking()
            .Where(x => requestBytes.SequenceEqual(x.Request)) // compare request with owner
            .FirstOrDefaultAsync(x => x.Function == "ScrapeJobs");

        if (scrapeJobTicker is null)
            return;

        config.ScrapeCron = scrapeJobTicker.Expression;
    }

    private async Task SaveConfig()
    {
        if (isWorking || !await validator.ValidateAsync())
            return;

        isWorking = true;
        await UpdatePageAsync();

        if (config.Owner == "system") // check if it is a default value
            dbContext.Add(config);
        else
            dbContext.Update(config);

        await dbContext.SaveChangesAsync();

        isWorking = false;
        PushNotification("Configuration saved successfully."); // Disabled notification as requested
    }

    private async Task StartScraping()
    {
        if (isWorking || !await validator.ValidateAsync())
            return;

        await SaveConfig();

        if (ScrapeHandler.ScrapeSemaphore.CurrentCount == 0)
        {
            PushNotification("Scraping process is already in progress. " +
                "Probably another user is scraping. Try again later.",
                ToastType.Warning);

            return;
        }

        isWorking = true;
        PushNotification("Scraping in progress...");
        await UpdatePageAsync();

        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var scrapeHandler = scope.ServiceProvider.GetRequiredService<ScrapeHandler>();
            var sources = config.Sources.Where(x => !x.Disabled).ToArray();

            var newOffersCount = await scrapeHandler.ScrapeLists(sources, cts.Token);
            PushNotification("Scraping details...");
            var result = await scrapeHandler.ScrapeDetails(sources, cts.Token);

            var aiSummaryEnabled = dbContext.AiSummaryConfigs.Any(x => x.AiSummaryEnabled == true);
            if (aiSummaryEnabled)
            {
                dbContext.ScheduleAiSummary(DateTime.UtcNow.AddMinutes(1));
                await dbContext.SaveChangesAsync();
                PushNotification("Offer AI summary scheduled for new offers...");
            }

            PushScrapeFinishNotif(newOffersCount, result?.Length ?? 0);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during the scraping process");
            PushNotification($"Error: {ex.Message}", ToastType.Danger);
        }
        finally
        {
            isWorking = false;
            await UpdatePageAsync();
        }
    }
    private void PushScrapeFinishNotif(int newOffersCount, int offerDetailsScrapedCount)
    {
        var sourcesCount = config.Sources.Count(x => !x.Disabled);
        var message = $"Scrape finished successfully for number of sources: {sourcesCount}. New offers: {newOffersCount}. ";
        if (offerDetailsScrapedCount > 0)
            message += $"Offer details scraped: {offerDetailsScrapedCount}";

        PushNotification(message);
    }

    private async Task UpdatePageAsync()
    {
        StateHasChanged();
        await Task.Yield();
    }

    private async Task RefreshKeywordsOnOffers()
    {
        if (isWorking || !await validator.ValidateAsync())
            return;

        await SaveConfig();

        isWorking = true;
        PushNotification("Refreshing offers in progress...");
        await UpdatePageAsync();

        try
        {
            using var scope = serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var result = await mediator.Send(new RefreshKeywordsOnOffers.Command());

            PushNotification($"Refreshing offers finished successfully with {result.ChangeCount} updates.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during the refreshing offers");
            PushNotification($"Error: {ex.Message}");
        }
        finally
        {
            isWorking = false;
            await UpdatePageAsync();
        }
    }

    private async Task ScheduleScraping()
    {
        if (string.IsNullOrEmpty(config.ScrapeCron))
        {
            PushNotification("Please configure the cron expression before scheduling;", ToastType.Warning);
            return;
        }

        if (isWorking || !await validator.ValidateAsync())
            return;

        await SaveConfig();

        isWorking = true;

        if (scrapeJobTicker is not null)
        {
            dbContext.Remove(scrapeJobTicker);
            scrapeJobTicker = null;
        }

        var cronTickerEntity = new CronTickerEntity
        {
            Expression = config.ScrapeCron,
            Function = "ScrapeJobs",
            Description = "Scheduled in ScrapePage",
            Request = requestBytes,
            Retries = 1,
            RetryIntervals = [20], // set in seconds
        };

        await dbContext.AddAsync(cronTickerEntity);
        await dbContext.SaveChangesAsync();

        scrapeJobTicker = await dbContext.Set<CronTickerEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Function == "ScrapeJobs");

        PushNotification("Scraping scheduled correctly.");
        isWorking = false;
    }

    public void Dispose()
    {
        cts.Cancel();
        cts.Dispose();
    }
}
