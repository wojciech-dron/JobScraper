﻿using JobScraper.Models;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using PlaywrightExtraSharp;
using PlaywrightExtraSharp.Models;
using PlaywrightExtraSharp.Plugins.AnonymizeUa;
using PlaywrightExtraSharp.Plugins.ExtraStealth;
using PlaywrightExtraSharp.Plugins.ExtraStealth.Evasions;
using PlaywrightExtraSharp.Plugins.Recaptcha;
using Polly;
using Polly.Retry;
// ReSharper disable VirtualMemberCallInConstructor

namespace JobScraper.Logic.Common;

public abstract class ScrapperBase : IAsyncDisposable
{
    protected readonly ScraperConfig ScrapeConfig;
    protected readonly OriginConfig OriginConfig;
    protected readonly ILogger<ScrapperBase> Logger;
    private PlaywrightExtra? _playwright;

    protected abstract DataOrigin DataOrigin { get; }

    public bool IsEnabled { get; }
    public string BaseUrl { get; }

    public string SearchUrl => OriginConfig.SearchUrl;
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
        ScrapeConfig = config.Value;
        OriginConfig = ScrapeConfig.Providers.GetValueOrDefault(DataOrigin, new OriginConfig());
        IsEnabled = ScrapeConfig.IsEnabled(DataOrigin);

        var uri = new Uri(OriginConfig.SearchUrl);
        BaseUrl = $"{uri.Scheme}://{uri.Host}";
    }

    public async Task<IPage> NewPageAsync()
    {
        _playwright ??= await new PlaywrightExtra(ScrapeConfig.BrowserType)
            .Install()
            .Use(new StealthExtraPlugin(new StealthHardwareConcurrencyOptions(4)))
            .Use(new AnonymizeUaExtraPlugin())
            .LaunchAsync(new()
            {
                Headless = !ScrapeConfig.ShowBrowserWhenScraping
            });

        return await _playwright.NewPageAsync(new()
        {
            // UserAgent = UserAgentStrings[Random.Shared.Next() % UserAgentStrings.Length]
        });
    }

    protected async Task SaveScrenshoot(IPage page, string path)
    {
        path = PrepareDestination(path);

        var screenshot = await page.ScreenshotAsync(new() { FullPage = true });
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
        path = Path.Combine(ScrapeConfig.PageSavingDirectory, path);

        var directory = Path.GetDirectoryName(path);
        if (directory is not null)
            Directory.CreateDirectory(directory);

        return path;
    }

    protected async Task<IPage> LoadUntilAsync(string url,
        Func<IPage, Task<bool>>? successCondition = null,
        float waitSeconds = 5)
    {
        const int maxAttempts = 3;
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
                await page.Mouse.MoveAsync(200, 200, options: new MouseMoveOptions()
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

    protected List<string> FindMyKeywords(JobOffer jobOffer)
    {
        return ScrapeConfig.Keywords
            .Where(keyword => jobOffer.Description!.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async ValueTask DisposeAsync()
    {
        if (_playwright != null)
            await _playwright.DisposeAsync();

        _playwright = null;
    }
}