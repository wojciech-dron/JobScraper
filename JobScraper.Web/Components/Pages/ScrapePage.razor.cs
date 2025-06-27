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
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JobScraper.Web.Components.Pages;

public partial class ScrapePage
{
    private readonly IDbContextFactory<JobsDbContext> _dbFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ScrapePage> _logger;
    private JobsDbContext _dbContext = null!;

    private bool isWorking = false;
    private string statusMessage = "Ready for scraping.";
    private ScraperConfig config = new();

    public ScrapePage(IDbContextFactory<JobsDbContext> dbFactory,
        IServiceProvider serviceProvider,
        ILogger<ScrapePage> logger)
    {
        _dbFactory = dbFactory;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task OnInitializedAsync()
    {
        _dbContext = await _dbFactory.CreateDbContextAsync();

        var dbConfig = await _dbContext.ScraperConfigs.FirstOrDefaultAsync();
        if (dbConfig is null)
        {
            dbConfig = new ScraperConfig();
            _dbContext.ScraperConfigs.Add(config);
        }

        config = dbConfig;
    }

    private async Task SaveConfig()
    {
        if (isWorking)
            return;

        isWorking = true;
        statusMessage = "Saving configuration...";

        await _dbContext.SaveChangesAsync();

        isWorking = false;
        statusMessage = "Configuration saved successfully.";
    }

    private async Task StartScraping()
    {
        if (isWorking)
            return;

        await SaveConfig();

        isWorking = true;
        statusMessage = "Scraping in progress...";

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var sources = config.Sources;

            var listCommands = sources.Select<SourceConfig, ScrapeCommand>(source => source.DataOrigin switch
            {
                DataOrigin.Indeed      => new IndeedListScraper.Command { Source = source },
                DataOrigin.JustJoinIt  => new JjitListScraper.Command { Source = source },
                DataOrigin.NoFluffJobs => new NoFluffJobsListScraper.Command { Source = source },
                DataOrigin.PracujPl    => new PracujPlListScraper.Command { Source = source },
                DataOrigin.RocketJobs  => new RocketJobsListScraper.Command { Source = source },
                DataOrigin.Olx         => new OlxListScraper.Command { Source = source },
                _                      => throw new NotImplementedException($"List scraping not implemented for {source}")
            }).ToArray();

            for (int idx = 0; idx < listCommands.Length; idx++)
            {
                var command = listCommands[idx];
                statusMessage = $"Scraping list pages of source: {idx + 1}/{listCommands.Length}";
                StateHasChanged();

                await mediator.SendWithRetry(command);
            }

            // details
            var detailsCommands = sources.Select<SourceConfig, ScrapeCommand?>(source => source.DataOrigin switch
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

            for (int idx = 0; idx < detailsCommands.Length; idx++)
            {
                var command = detailsCommands[idx]!;
                statusMessage = $"Scraping details pages of source: {idx + 1}/{detailsCommands.Length}";
                StateHasChanged();

                await mediator.SendWithRetry(command);
            }

            statusMessage = "Scrape finished successfully";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during the scraping process");
            statusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            isWorking = false;
            StateHasChanged();
        }
    }
}

