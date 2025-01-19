using Cocona;
using JobScraper;
using JobScraper.Logic;
using JobScraper.Persistance;
using JobScraper.Persistence;
using JobScraper.Scrapers;
using JobScraper.Scrapers.JustJoinIt;
using MediatR;
using Microsoft.EntityFrameworkCore;

var builder = CoconaApp.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);

await builder.Services
    .AddSqlitePersistance()
    .AddScrapperServicesAsync(builder.Configuration);

var app = builder.Build();
app.Services.PrepareDb();

app.Run<Commands>();

Console.WriteLine("Scrapper finished");


public class Commands
{
    private readonly JobsDbContext _dbContext;
    private readonly ILogger<Commands> _logger;
    private readonly IMediator _mediator;
    public Commands(JobsDbContext dbContext,
        ILogger<Commands> logger,
        IMediator mediator)
    {
        _dbContext = dbContext;
        _logger = logger;
        _mediator = mediator;

    }

    [Command("indeed-list")]
    public async Task GetNewJobs([FromService] IndeedListScraper scraper)
    {
        _logger.LogInformation("Scraping Indeed jobs list...");

        await foreach (var jobs in scraper.ScrapeJobs())
        {
            _logger.LogInformation("Syncing Indeed jobs...");
            await _mediator.Send(new SyncJobsFromList.Command(jobs));
        }

    }

    [Command("jjit-list")]
    public async Task GetNewJobs([FromService] JjitListScraper scraper)
    {
        _logger.LogInformation("Scraping Just-join.it jobs list...");

        await foreach (var jobs in scraper.ScrapeJobs())
        {
            _logger.LogInformation("Syncing Just-join.it jobs...");
            await _mediator.Send(new SyncJobsFromList.Command(jobs));
        }
    }

    [Command("indeed-details")]
    public async Task RetryEmptyDetails([FromService] IndeedDetailsScraper scraper)
    {
        var jobs = await _dbContext.JobOffers
            .Include(j => j.Company)
            .Where(j => j.Description == null)
            .ToListAsync();

        foreach (var job in jobs)
        {
            _logger.LogInformation("Scraping job: {JobTitle}", job.Title);
            await ScrapperBase.RetryPolicy.ExecuteAsync(async () =>
                await scraper.ScrapeJobDetails(job));
        }

        await _dbContext.SaveChangesAsync();
    }
}