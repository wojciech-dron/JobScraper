using Microsoft.Playwright;
using JobScraper.Data;
using JobScraper.Scrapers;
using Microsoft.EntityFrameworkCore;

var searchTerms = new List<string> { ".NET" };

using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Firefox.LaunchAsync();

var browserContext = await browser.NewContextAsync(new BrowserNewContextOptions
{
    UserAgent = "Mozilla/5.0 (Windows NT 10.0; rv:114.0) Gecko/20100101 Firefox/114.0"
});

await using var dbContext = new JobDbContext();
await dbContext.Database.EnsureCreatedAsync();

foreach (var searchTerm in searchTerms)
{
    Console.WriteLine($"Now scraping {searchTerm} jobs...");

    var indeedScraper = new IndeedScraper(browserContext);

    await GetNewJobs(indeedScraper, dbContext);
    // await RetryEmptyDetails(dbContext, indeedScraper);
}

await dbContext.SaveChangesAsync();
Console.WriteLine("All finished scraping :)");

async Task GetNewJobs(IndeedScraper indeedScraper2, JobDbContext dbContext1)
{
    var jobs = await indeedScraper2.ScrapeJobs();
    dbContext1.Jobs.AddRange(jobs);
    await dbContext1.SaveChangesAsync();
}

async Task RetryEmptyDetails(JobDbContext jobDbContext, IndeedScraper indeedScraper1)
{
    var jobs = await jobDbContext.Jobs.Where(j => j.Description == null).ToListAsync();
    foreach (var job in jobs)
    {
        Console.WriteLine($"Scraping job: {job.Title}.");
        await IndeedScraper.RetryPolicy.ExecuteAsync(async () =>
            await indeedScraper1.ScrapeJobDetails(job));
    }

    await jobDbContext.SaveChangesAsync();
}

