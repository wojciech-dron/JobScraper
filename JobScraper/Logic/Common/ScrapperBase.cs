﻿using JobScraper.Models;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using Polly;
using Polly.Retry;

namespace JobScraper.Logic.Common;

public abstract class ScrapperBase : IAsyncDisposable
{
    protected readonly ScraperConfig Config;
    protected readonly ILogger<ScrapperBase> Logger;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    protected abstract DataOrigin DataOrigin { get; }

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
            .WaitAndRetryAsync(2, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

    public ScrapperBase(IOptions<ScraperConfig> config,
        ILogger<ScrapperBase> logger)
    {
        Logger = logger;
        Config = config.Value;
    }

    public async Task<IBrowserContext> NewContextAsync()
    {
        _playwright ??= await Playwright.CreateAsync();
        _browser ??= await _playwright.Firefox.LaunchAsync();

        return await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            UserAgent = UserAgentStrings[Random.Shared.Next() % UserAgentStrings.Length]
        });
    }

    protected async Task SaveScrenshoot(IPage page, string path)
    {
        path = PrepareDestination(path);

        var screenshot = await page.ScreenshotAsync();
        await File.WriteAllBytesAsync(path, screenshot);
    }

    protected async Task SavePage(IPage page, string path)
    {
        path = PrepareDestination(path);

        var htmlContent = await page.ContentAsync();
        await File.WriteAllTextAsync(path, htmlContent);
    }

    private string PrepareDestination(string path)
    {
        path = Path.Combine(Config.PageSavingDirectory, path);

        var directory = Path.GetDirectoryName(path);
        if (directory is not null)
            Directory.CreateDirectory(directory);

        return path;
    }

    protected async Task<IPage> LoadUntilAsync(string url,
        Func<IPage, Task<bool>>? successCondition = null,
        float waitSeconds = 5)
    {
        const int maxAttempts = 5;
        successCondition ??= async p => (await p.QuerySelectorAllAsync("main.error")).Count == 0;

        IPage page;
        var retryAttempts = 0;
        do
        {
            var context = await NewContextAsync();
            page = await context.NewPageAsync();
            await page.GotoAsync(url);
            await page.WaitForTimeoutAsync(waitSeconds * 1000);

            retryAttempts++;
        } while (retryAttempts < maxAttempts && !await successCondition(page));

        if (retryAttempts == maxAttempts)
            throw new ApplicationException("Retry attempts exceeded.");

        return page;
    }

    protected List<string> FindMyKeywords(JobOffer jobOffer)
    {
        return Config.Keywords
            .Where(keyword => jobOffer.Description!.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async ValueTask DisposeAsync()
    {
        _playwright?.Dispose();
        _playwright = null;
        if (_browser is not null)
            await _browser.DisposeAsync();
        _browser = null;
    }
}