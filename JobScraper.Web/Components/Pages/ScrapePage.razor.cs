using Blazored.FluentValidation;
using JobScraper.Common.Extensions;
using JobScraper.Logic;
using JobScraper.Logic.Common;
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

namespace JobScraper.Web.Components.Pages;

public partial class ScrapePage
{
    private readonly IDbContextFactory<JobsDbContext> _dbFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ScrapePage> _logger;
    private readonly AppSettings appSettings;
    private JobsDbContext dbContext = null!;
    private FluentValidationValidator validator = null!;

    private bool isWorking = false;
    private string statusMessage = "Ready for scraping.";
    private ScraperConfig config = new();

    public ScrapePage(IDbContextFactory<JobsDbContext> dbFactory,
        IServiceProvider serviceProvider,
        IOptions<AppSettings> appSettings,
        ILogger<ScrapePage> logger)
    {
        _dbFactory = dbFactory;
        _serviceProvider = serviceProvider;
        this.appSettings = appSettings.Value;
        _logger = logger;
    }

    protected override async Task OnInitializedAsync()
    {
        dbContext = await _dbFactory.CreateDbContextAsync();
        config = await dbContext.ScraperConfigs.FirstOrDefaultAsync() ?? new ScraperConfig();
    }

    private async Task SaveConfig()
    {
        if (isWorking || !await validator.ValidateAsync())
            return;

        isWorking = true;
        statusMessage = "Saving configuration...";
        await UpdatePageAsync();

        if (config.Id == 0)
        {
            dbContext.Add(config);
        }
        else
        {
            dbContext.Update(config);
        }

        await dbContext.SaveChangesAsync();

        isWorking = false;
        statusMessage = "Configuration saved successfully.";
    }

    private async Task StartScraping()
    {
        if (isWorking || !await validator.ValidateAsync())
            return;

        await SaveConfig();

        isWorking = true;
        statusMessage = "Scraping in progress...";
        await UpdatePageAsync();

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var sources = config.Sources.Where(x => !x.Disabled).ToArray();

            var newOffersCount = await ScrapeLists(sources, mediator);

            await ScrapeDetails(sources, mediator);

            // Add new offers count
            statusMessage = $"Scrape finished successfully for number of sources: {sources.Length}. " +
                $"New offers: {newOffersCount}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during the scraping process");
            statusMessage = $"Error: {ex.Message}";
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
                DataOrigin.Indeed      => new IndeedListScraper.Command { Source = source },
                DataOrigin.JustJoinIt  => new JjitListScraper.Command { Source = source },
                DataOrigin.NoFluffJobs => new NoFluffJobsListScraper.Command { Source = source },
                DataOrigin.PracujPl    => new PracujPlListScraper.Command { Source = source },
                DataOrigin.RocketJobs  => new RocketJobsListScraper.Command { Source = source },
                DataOrigin.Olx         => new OlxListScraper.Command { Source = source },
                _                      => throw new ArgumentOutOfRangeException($"List scraping not implemented for {source}")
            }).ToArray();

        var offersCount = 0;
        for (int idx = 0; idx < listCommands.Length; idx++)
        {
            var command = listCommands[idx];
            statusMessage = $"Scraping list pages of source: {idx + 1}/{listCommands.Length}";
            await UpdatePageAsync();

            var result = await mediator.SendWithRetry(command, logger: _logger);
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
                DataOrigin.Indeed      => new IndeedDetailsScraper.Command { Source = source },
                DataOrigin.JustJoinIt  => new JjitDetailsScraper.Command { Source = source },
                DataOrigin.NoFluffJobs => new NoFluffJobsDetailsScraper.Command { Source = source },
                DataOrigin.RocketJobs  => new RocketJobsDetailsScraper.Command { Source = source },
                DataOrigin.PracujPl    => null,
                DataOrigin.Olx         => null,

                _ => throw new NotImplementedException($"List scraping not implemented for {source}")
            }).Where(c => c is not null)
            .ToArray();

        var offersCount = 0;
        for (int idx = 0; idx < detailsCommands.Length; idx++)
        {
            var command = detailsCommands[idx]!;
            statusMessage = $"Scraping details pages of source: {idx + 1}/{detailsCommands.Length}";
            await UpdatePageAsync();

            var result = await mediator.SendWithRetry(command, logger: _logger);
            offersCount += result.ScrapedOffersCount;
        }

        return offersCount;
    }

    private async Task UpdatePageAsync()
    {
        StateHasChanged();
        await Task.Yield();
    }
}

