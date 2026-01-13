using BlazorBootstrap;
using Blazored.FluentValidation;
using JobScraper.Common.Extensions;
using JobScraper.Logic.Common;
using JobScraper.Logic.Functions;
using JobScraper.Logic.Indeed;
using JobScraper.Logic.Jjit;
using JobScraper.Logic.NoFluffJobs;
using JobScraper.Logic.Olx;
using JobScraper.Logic.PracujPl;
using JobScraper.Logic.RocketJobs;
using JobScraper.Models;
using JobScraper.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using TickerQ.Utilities.Entities;

namespace JobScraper.Web.Pages;

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
        ShowNotification("Configuration saved successfully."); // Disabled notification as requested
    }

    private async Task StartScraping()
    {
        if (isWorking || !await validator.ValidateAsync())
            return;

        await SaveConfig();

        isWorking = true;
        ShowNotification("Scraping in progress...");
        await UpdatePageAsync();

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var sources = config.Sources.Where(x => !x.Disabled).ToArray();

            var newOffersCount = await ScrapeLists(sources, mediator);
            var offerDetailsScrapedCount = await ScrapeDetails(sources, mediator);

            // Add new offers count
            var message = $"Scrape finished successfully for number of sources: {sources.Length}. New offers: {newOffersCount}. ";
            if (offerDetailsScrapedCount > 0)
                message += $"Offer details scraped: {offerDetailsScrapedCount}";

            ShowNotification(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during the scraping process");
            ShowNotification($"Error: {ex.Message}", ToastType.Danger);
        }
        finally
        {
            isWorking = false;
            await UpdatePageAsync();
        }
    }

    private async Task<int> ScrapeLists(IEnumerable<SourceConfig> sources, IMediator mediator)
    {
        var listCommands = sources
            .Where(s => !s.Disabled)
            .Select<SourceConfig, ScrapeCommand>(source => source.DataOrigin switch
            {
                DataOrigin.Indeed => new IndeedListScraper.Command
                {
                    Source = source,
                },
                DataOrigin.JustJoinIt => new JjitListScraper.Command
                {
                    Source = source,
                },
                DataOrigin.NoFluffJobs => new NoFluffJobsListScraper.Command
                {
                    Source = source,
                },
                DataOrigin.PracujPl => new PracujPlListScraper.Command
                {
                    Source = source,
                },
                DataOrigin.RocketJobs => new RocketJobsListScraper.Command
                {
                    Source = source,
                },
                DataOrigin.Olx => new OlxListScraper.Command
                {
                    Source = source,
                },
                _ => throw new ArgumentOutOfRangeException($"List scraping not implemented for {source}"),
            }).ToArray();

        var offersCount = 0;
        for (var idx = 0; idx < listCommands.Length; idx++)
        {
            var command = listCommands[idx];
            ShowNotification($"Scraping list pages of source: {idx + 1}/{listCommands.Length}");
            await UpdatePageAsync();

            var result = await mediator.Send(command);
            offersCount += result.ScrapedOffersCount;
        }

        return offersCount;
    }

    private async Task<int> ScrapeDetails(IEnumerable<SourceConfig> sources, IMediator mediator)
    {
        var detailsCommands = sources
            .Where(s => !s.Disabled)
            .Select<SourceConfig, ScrapeCommand?>(source => source.DataOrigin switch
            {
                DataOrigin.Indeed => new IndeedDetailsScraper.Command
                {
                    Source = source,
                },
                DataOrigin.JustJoinIt => new JjitDetailsScraper.Command
                {
                    Source = source,
                },
                DataOrigin.NoFluffJobs => new NoFluffJobsDetailsScraper.Command
                {
                    Source = source,
                },
                DataOrigin.RocketJobs => new RocketJobsDetailsScraper.Command
                {
                    Source = source,
                },
                DataOrigin.PracujPl => null,
                DataOrigin.Olx      => null,
                _                   => throw new ArgumentOutOfRangeException($"List scraping not implemented for {source}"),
            }).Where(c => c is not null)
            .ToArray();

        var offersCount = 0;
        for (var idx = 0; idx < detailsCommands.Length; idx++)
        {
            var command = detailsCommands[idx]!;
            ShowNotification($"Scraping details pages of source: {idx + 1}/{detailsCommands.Length}");
            await UpdatePageAsync();

            var result = await mediator.SendWithRetry(command, _logger);
            offersCount += result.ScrapedOffersCount;
        }

        return offersCount;
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
        ShowNotification("Refreshing offers in progress...");
        await UpdatePageAsync();

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var result = await mediator.Send(new RefreshKeywordsOnOffers.Command());

            ShowNotification($"Refreshing offers finished successfully with {result.ChangeCount} updates.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during the refreshing offers");
            ShowNotification($"Error: {ex.Message}");
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
            ShowNotification("Please configure the cron expression before scheduling;", ToastType.Warning);
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

        ShowNotification("Scraping scheduled correctly.");
        isWorking = false;
    }
}
