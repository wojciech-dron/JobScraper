using JobScraper.Web.Modules.Jobs;

namespace JobScraper.Web.Common.Entities;

public class AppSettings
{
    public const string SectionName = "AppSettings";

    public string PageSavingDirectory { get; set; } = @".\Data\jobs";
    public bool ContainerizedApp { get; init; } = false;
    public PlaywrightModeEnum PlaywrightMode { get; init; }
    public string? CdpEndpointUrl { get; init; }

    public BrowserTypeEnum[] AllowedBrowsers()
    {
        if (PlaywrightMode is PlaywrightModeEnum.OverCdp)
            return [BrowserTypeEnum.Chromium];

        if (PlaywrightMode is PlaywrightModeEnum.Local && ContainerizedApp)
            return [BrowserTypeEnum.Firefox];

        return Enum.GetValues<BrowserTypeEnum>();
    }

    public TickerConfig? TickerQ { get; set; }
}

public enum PlaywrightModeEnum
{
    Local,
    OverCdp,
}
