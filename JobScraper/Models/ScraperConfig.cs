namespace JobScraper.Models;

public class ScraperConfig
{
    public static string SectionName => "ScraperConfig";

    public string JjitBaseUrl { get; set; } = "https://justjoin.it";
    public string? JjitSearchUrl { get; set; }

    public string IndeedBaseUrl { get; set; } = "https://www.indeed.com";
    public string? IndeedSearchUrl { get; set; }

    public string SearchTerm { get; set; } = "C#";
    public string Location { get; set; } = "";
    public int ListingAgeInDays { get; set; } = 15;
    public bool RemoteJobsOnly { get; set; }

    public float WaitForListSeconds { get; set; } = 10;
    public float WaitForScrollSeconds { get; set; } = 4;
    public float WaitForDetailsSeconds { get; set; } = 5;

    public string[] Keywords { get; set; } = [];
    public string[] AvoidJobKeywords { get; set; } = [];

    public string PageSavingDirectory { get; set; } = ".\\Data\\jobs";
}