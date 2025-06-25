using PlaywrightExtraSharp.Models;

namespace JobScraper.Models;

public class ScraperConfig
{
    public static string SectionName => "ScraperConfig";

    public string PageSavingDirectory { get; set; } = @".\Data\jobs";
    public float WaitForListSeconds { get; set; } = 10;
    public float WaitForScrollSeconds { get; set; } = 4;
    public float WaitForDetailsSeconds { get; set; } = 5;

    public bool ShowBrowserWhenScraping { get; set; }
    public BrowserTypeEnum BrowserType { get; set; } = BrowserTypeEnum.Chromium;
    public string[] Keywords { get; set; } = [];

    public Dictionary<DataOrigin, OriginConfig> Providers { get; set; } = new();

    public DataOrigin[] GetEnabledOrigins() => Providers.Keys.ToArray();
    public bool IsEnabled(DataOrigin origin) => Providers.ContainsKey(origin);
}

public class OriginConfig
{
    public string SearchUrl { get; set; } = "";
}
