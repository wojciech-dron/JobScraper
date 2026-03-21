using JobScraper.Web.Common.Entities;
using JobScraper.Web.Common.Models;
using Microsoft.Playwright;

namespace JobScraper.Web.Features.JobOffers.Scrape.Logic.Common;

public interface IPageFactory : IAsyncDisposable
{
    Task<IPage> NewPageAsync();
}

internal sealed class DefaultPageFactory : IPageFactory
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

    private IBrowser? _browser;
    private IPlaywright? _playwright;

    public required AppSettings AppSettings { get; set; }
    public required ScraperConfig ScrapeConfig { get; set; }

    public async ValueTask DisposeAsync()
    {
        _playwright?.Dispose();
        _playwright = null;

        if (_browser is not null)
        {
            await _browser.DisposeAsync();
            _browser = null;
        }
    }

    public async Task<IPage> NewPageAsync()
    {
        await DisposeAsync();

        _playwright = await Playwright.CreateAsync();

        if (AppSettings is { ContainerizedApp: false, PlaywrightMode: PlaywrightModeEnum.Local })
            Install();

        var browserType = ScrapeConfig.BrowserType switch
        {
            BrowserTypeEnum.Chromium => _playwright.Chromium,
            BrowserTypeEnum.Firefox  => _playwright.Firefox,
            BrowserTypeEnum.Webkit   => _playwright.Webkit,
            _                        => throw new ArgumentOutOfRangeException(nameof(ScrapeConfig), ScrapeConfig.BrowserType, null),
        };

        _browser = AppSettings.PlaywrightMode switch
        {
            PlaywrightModeEnum.OverCdp => await ConnectOverCdpAsync(browserType),
            PlaywrightModeEnum.Local => await LaunchLocalBrowser(browserType),
            _ => throw new ArgumentOutOfRangeException(nameof(AppSettings), AppSettings.PlaywrightMode, null),
        };

        var page = await _browser.NewPageAsync(new BrowserNewPageOptions
        {
            UserAgent = UserAgentStrings[Random.Shared.Next() % UserAgentStrings.Length],
        });

        page.SetDefaultTimeout(5           * 60 * 1000);
        page.SetDefaultNavigationTimeout(5 * 60 * 1000);

        return page;
    }

    private void Install()
    {
        var num = Microsoft.Playwright.Program.Main([
            "install",
            "--with-deps",
            ScrapeConfig.BrowserType.ToString().ToLower(),
        ]);

        if (num != 0)
            throw new Exception($"Playwright exited with code {num}");
    }

    private async Task<IBrowser> LaunchLocalBrowser(IBrowserType browserType)
        => await browserType.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = !ScrapeConfig.ShowBrowserWhenScraping,
        });

    private async Task<IBrowser> ConnectOverCdpAsync(IBrowserType browserType)
    {
        if (string.IsNullOrWhiteSpace(AppSettings.CdpEndpointUrl))
            throw new ArgumentException("Playwright connection string is not set");

        return await browserType.ConnectOverCDPAsync(AppSettings.CdpEndpointUrl, new BrowserTypeConnectOverCDPOptions());
    }
}
