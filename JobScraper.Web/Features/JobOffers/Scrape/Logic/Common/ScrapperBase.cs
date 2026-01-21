using JobScraper.Web.Common.Entities;
using JobScraper.Web.Modules.Persistence;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using Polly;
using Polly.Retry;

// ReSharper disable VirtualMemberCallInConstructor

namespace JobScraper.Web.Features.JobOffers.Scrape.Logic.Common;

public abstract class ScrapperBase : IDisposable
{

    private static readonly string[] UserAgentStrings =
    [
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.2227.0 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/110.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.3497.92 Safari/537.36",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/110.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Linux; Android 12) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/110.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/111.0.0.0 Safari/537.36",
    ];

    public static readonly AsyncRetryPolicy<JobOffer> RetryPolicy =
        Policy<JobOffer>.Handle<Exception>()
            .WaitAndRetryAsync(1, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    protected readonly AppSettings AppSettings;
    protected readonly JobsDbContext DbContext;
    protected readonly ILogger<ScrapperBase> Logger;
    protected readonly ScraperConfig ScrapeConfig;
    protected readonly SourceConfig Source;
    private IBrowser? _browser;
    private IPlaywright? _playwright;

    protected abstract DataOrigin DataOrigin { get; }

    public bool IsEnabled { get; }
    public string BaseUrl { get; }

    public ScrapperBase(IOptions<AppSettings> appSettings,
        ILogger<ScrapperBase> logger,
        JobsDbContext dbContext)
    {
        AppSettings = appSettings.Value;
        DbContext = dbContext;
        Logger = logger;
        ScrapeConfig = DbContext.ScraperConfigs.First();
        Source = ScrapeConfig.Sources.FirstOrDefault(x => x.DataOrigin == DataOrigin) ?? new SourceConfig();
        IsEnabled = ScrapeConfig.IsEnabled(DataOrigin);

        var uri = new Uri(Source.SearchUrl);
        BaseUrl = $"{uri.Scheme}://{uri.Host}";
    }


#pragma warning disable CA1862, CA2012, CA1816
    public void Dispose()
    {
        if (_playwright is not null)
        {
            _playwright.Dispose();
            _playwright = null;
        }

        if (_browser is not null)
        {
            _browser.DisposeAsync().GetAwaiter().GetResult();
            _browser = null;
        }
    }

#pragma warning restore CA1862, CA2012, CA1816
    public async Task<IPage> NewPageAsync()
    {
        Dispose();

        _playwright = await Playwright.CreateAsync();

        if (!AppSettings.PreinstalledPlaywright)
            Install();

        var browserType = ScrapeConfig.BrowserType switch
        {
            BrowserTypeEnum.Chromium => _playwright.Chromium,
            BrowserTypeEnum.Firefox  => _playwright.Firefox,
            BrowserTypeEnum.Webkit   => _playwright.Webkit,
            _                        => throw new ArgumentOutOfRangeException(nameof(ScrapeConfig.BrowserType)),
        };

        _browser = await browserType.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = !ScrapeConfig.ShowBrowserWhenScraping,
        });

        return await _browser.NewPageAsync(new BrowserNewPageOptions
        {
            UserAgent = UserAgentStrings[Random.Shared.Next() % UserAgentStrings.Length],
        });
    }

    public void Install()
    {
        var num = Microsoft.Playwright.Program.Main([
            "install",
            "--with-deps",
            ScrapeConfig.BrowserType.ToString().ToLower(),
        ]);

        if (num != 0)
            throw new Exception($"Playwright exited with code {num}");
    }


    protected async Task SaveScreenshot(JobOffer jobOffer, IPage page)
    {
        if (!ScrapeConfig.SaveScreenshots)
            return;

        jobOffer.ScreenShotPath = $"{DataOrigin}/{jobOffer.CompanyName}/{DateTime.UtcNow:yyMMdd_HHmm}.png";
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

        jobOffer.HtmlPath = $"{DataOrigin}/{jobOffer.CompanyName}/{DateTime.UtcNow:yyMMdd_HHmm}.html";
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
                await SavePage(page, Path.Combine($"{DataOrigin}", "error", $"{DateTime.Now:hh_mm}.html"));
            }

            retryAttempts++;
        } while (retryAttempts < maxAttempts && !await successCondition(page));

        if (retryAttempts == maxAttempts)
            throw new ApplicationException("Retry attempts exceeded");

        return page;
    }
}
