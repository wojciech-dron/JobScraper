using BlazorBootstrap;
using Blazored.FluentValidation;
using JobScraper.Web.Common.Entities;
using JobScraper.Web.Features.JobOffers.Scrape.Logic.Functions;
using JobScraper.Web.Modules.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using TickerQ.Utilities.Entities;

namespace JobScraper.Web.Features.JobOffers.Scrape;

public partial class ScrapePage
{
    private readonly IDbContextFactory<JobsDbContext> _dbFactory;
    private readonly IJSRuntime _js;
    private readonly ILogger<ScrapePage> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly AppSettings appSettings;
    private ScraperConfig config = new();
    private JobsDbContext dbContext = null!;

    private bool isWorking;
    private CronTickerEntity? scrapeJobTicker;
    private FluentValidationValidator validator = null!;

    public ScrapePage(IDbContextFactory<JobsDbContext> dbFactory,
        IServiceProvider serviceProvider,
        IJSRuntime js,
        IOptions<AppSettings> appSettings,
        ILogger<ScrapePage> logger)
    {
        _dbFactory = dbFactory;
        _serviceProvider = serviceProvider;
        _js = js;
        this.appSettings = appSettings.Value;
        _logger = logger;
    }

    protected override async Task OnInitializedAsync()
    {
        dbContext = await _dbFactory.CreateDbContextAsync();
        config = await dbContext.ScraperConfigs.FirstOrDefaultAsync() ?? new ScraperConfig();

        scrapeJobTicker = await dbContext.Set<CronTickerEntity>()
            .AsNoTracking()
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

        if (config.Id == 0)
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

        isWorking = true;
        PushNotification("Scraping in progress...");
        await UpdatePageAsync();

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var scrapeHandler = scope.ServiceProvider.GetRequiredService<ScrapeHandler>();
            var sources = config.Sources.Where(x => !x.Disabled).ToArray();

            var newOffersCount = await scrapeHandler.ScrapeLists(sources);
            PushNotification("Scraping details...");
            var offerDetailsScrapedCount = await scrapeHandler.ScrapeDetails(sources);

            PushScrapeFinishNotif(newOffersCount, offerDetailsScrapedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during the scraping process");
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
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var result = await mediator.Send(new RefreshKeywordsOnOffers.Command());

            PushNotification($"Refreshing offers finished successfully with {result.ChangeCount} updates.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during the refreshing offers");
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
}
