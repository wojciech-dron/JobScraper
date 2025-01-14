using Cocona;
using EFCore.BulkExtensions;
using JobScraper;
using JobScraper.Data;
using JobScraper.Scrapers;
using Microsoft.EntityFrameworkCore;

var builder = CoconaApp.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);

await builder.Services.AddScrapperServicesAsync(builder.Configuration);

var app = builder.Build();
app.Run<Commands>();

Console.WriteLine("Scrapper finished");


public class Commands
{
    private readonly JobDbContext _dbContext;
    private readonly ILogger<Commands> _logger;
    public Commands(JobDbContext dbContext,
        ILogger<Commands> logger)
    {
        _dbContext = dbContext;
        _logger = logger;

    }

    [Command("new")]
    public async Task GetNewJobs([FromService] IndeedListScraper scraper)
    {
        _logger.LogInformation("Now scraping jobs...");

        var jobs = await scraper.ScrapeJobs();
        await _dbContext.BulkInsertOrUpdateAsync(jobs);
    }

    [Command("details")]
    public async Task RetryEmptyDetails([FromService] IndeedDetailsScraper scraper)
    {
        var jobs = await _dbContext.Jobs
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