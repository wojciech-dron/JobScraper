using JobScraper.Web.Common.Entities;
using JobScraper.Web.Common.Models;
using JobScraper.Web.Modules.Persistence;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace JobScraper.Web.Features.JobOffers.Scrape.Logic.Common;

public abstract class ScraperBaseHandler(
    IOptions<AppSettings> appSettings,
    ILogger<ScraperBaseHandler> logger,
    JobsDbContext dbContext)
    : IAsyncDisposable
{
    protected readonly AppSettings AppSettings = appSettings.Value;
    protected readonly JobsDbContext DbContext = dbContext;
    protected readonly ILogger<ScraperBaseHandler> Logger = logger;

    internal IPageFactory? PageFactory { get; set; }

    protected ScraperConfig ScrapeConfig { get; set; } = null!;
    protected SourceConfig SourceConfig { get; set; } = null!;
    protected string Origin => SourceConfig.DataOrigin;

    public bool IsEnabled => ScrapeConfig.IsEnabled(Origin);

    public string BaseUrl => Uri.TryCreate(SourceConfig.SearchUrl, UriKind.Absolute, out var uri)
        ? $"{uri.Scheme}://{uri.Host}"
        : "";

    public async ValueTask DisposeAsync()
    {
        if (PageFactory is not null)
            await PageFactory.DisposeAsync();

        GC.SuppressFinalize(this);
    }

    public Task<IPage> NewPageAsync()
    {
        PageFactory ??= new DefaultPageFactory()
        {
            AppSettings = AppSettings,
            ScrapeConfig = ScrapeConfig,
        };

        return PageFactory.NewPageAsync();
    }

    protected async Task SaveScreenshot(JobOffer jobOffer, IPage page)
    {
        if (!ScrapeConfig.SaveScreenshots)
            return;

        jobOffer.ScreenShotPath = $"{Origin}/{jobOffer.CompanyName}/{DateTime.UtcNow:yyMMdd_HHmm}.png";
        await SaveScreenshot(page, jobOffer.ScreenShotPath);
    }

    protected async Task SaveScreenshot(IPage page, string path)
    {
        if (!ScrapeConfig.SaveScreenshots)
            return;

        path = PrepareDestination(path);

        var screenshot = await page.ScreenshotAsync(new PageScreenshotOptions
        {
            FullPage = true,
        });
        await File.WriteAllBytesAsync(path, screenshot);
    }

    protected async Task SavePage(JobOffer jobOffer, IPage page)
    {
        if (!ScrapeConfig.SavePages)
            return;

        jobOffer.HtmlPath = $"{Origin}/{jobOffer.CompanyName}/{DateTime.UtcNow:yyMMdd_HHmm}.html";
        await SavePage(page, jobOffer.HtmlPath);
    }

    protected async Task SavePage(IPage page, string path)
    {
        if (!ScrapeConfig.SavePages)
            return;

        path = PrepareDestination(path);

        var htmlContent = await page.ContentAsync();
        await File.WriteAllTextAsync(path, htmlContent);
    }

    private string PrepareDestination(string path)
    {
        path = Path.Combine(AppSettings.PageSavingDirectory, path);

        var directory = Path.GetDirectoryName(path);
        if (directory is not null)
            Directory.CreateDirectory(directory);

        return path;
    }

    protected async Task<IPage> LoadUntilAsync(string url,
        Func<IPage, Task<bool>>? successCondition = null,
        float waitSeconds = 5)
    {
        const int maxAttempts = 2;
        successCondition ??= async p => (await p.QuerySelectorAllAsync("main.error")).Count == 0;

        IPage page;
        var retryAttempts = 0;
        do
        {
            page = await NewPageAsync();
            await page.GotoAsync(url);
            await page.WaitForTimeoutAsync(waitSeconds * 1000);

            if (retryAttempts > 0)
            {
                await page.Mouse.MoveAsync(200,
                    200,
                    new MouseMoveOptions
                    {
                        Steps = 5,
                    });
                await SavePage(page, Path.Combine($"{Origin}", "error", $"{DateTime.Now:hh_mm}.html"));
            }

            retryAttempts++;
        } while (retryAttempts < maxAttempts && !await successCondition(page));

        if (retryAttempts == maxAttempts)
            throw new ApplicationException("Retry attempts exceeded");

        return page;
    }
}
