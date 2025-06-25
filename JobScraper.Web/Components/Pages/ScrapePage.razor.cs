using JobScraper.Logic;
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

    private bool isScraping = false;
    private string statusMessage = "";
    private List<OriginConfigViewModel> providers { get; set; } = [];
    private ScraperConfig config = new();

    public ScrapePage(IDbContextFactory<JobsDbContext> dbFactory,
        IServiceProvider serviceProvider,
        ILogger<ScrapePage> logger)
    {
        _dbFactory = dbFactory;
        _serviceProvider = serviceProvider;
        _logger = logger;

    }

    private async Task StartScraping()
    {
        if (isScraping)
            return;

        isScraping = true;
        statusMessage = "Scraping in progress... You can observe progress in the console logs.";

        try
        {
            using var scope = ServiceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var request = new ScrapePipeline.Request();
            await mediator.Send(request, CancellationToken.None);

            statusMessage = "Scrape finished successfully";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An error occurred during the scraping process");
            statusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            isScraping = false;
        }
    }

    private class OriginConfigViewModel : OriginConfig
    {
        public DataOrigin DataOrigin { get; set; } = DataOrigin.PracujPl;
    }
}

