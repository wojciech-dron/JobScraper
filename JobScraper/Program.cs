using Microsoft.Playwright;
using JobScraper.Data;
using JobScraper.Scrapers;

var searchTerms = new List<string> { ".NET" };

using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Firefox.LaunchAsync();

var context = await browser.NewContextAsync(new BrowserNewContextOptions 
{ 
    UserAgent = "Mozilla/5.0 (Windows NT 10.0; rv:114.0) Gecko/20100101 Firefox/114.0" 
});

await using var dbContext = new JobDbContext();
await dbContext.Database.EnsureCreatedAsync();

foreach (var searchTerm in searchTerms)
{
    Console.WriteLine($"Now scraping {searchTerm} jobs...");

    var indeedScraper = new IndeedScraper(searchTerm, 1, context);
    var indeedJobs = await indeedScraper.ScrapeJobsAsync();
    dbContext.Jobs.AddRange(indeedJobs);
}

await dbContext.SaveChangesAsync();
Console.WriteLine("All finished scraping :)");
