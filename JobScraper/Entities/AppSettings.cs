namespace JobScraper.Entities;

public class AppSettings
{
    public const string SectionName = "AppSettings";

    public string PageSavingDirectory { get; set; } = @".\Data\jobs";
    public bool PreinstalledPlaywright { get; init; } = false;

    public BrowserTypeEnum[] AllowedBrowsers => PreinstalledPlaywright
        ? [BrowserTypeEnum.Firefox] // preinstalled in Dockerfile
        : Enum.GetValues<BrowserTypeEnum>();
}