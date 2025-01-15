namespace JobScraper.Models;

public class ScraperConfig
{
    public static string SectionName => "ScraperConfig";

    public string IndeedBaseUrl { get; set; } = "https://www.indeed.com";
    public string JjitBaseUrl { get; set; } = "https://justjoin.it";

    public string SearchTerm { get; set; } = "C#";
    public string Location { get; set; } = "";
    public int ListingAgeInDays { get; set; } = 3;
    public int WaitForListSeconds { get; set; } = 8;
    public int WaitForDetailsSeconds { get; set; } = 5;
    public bool RemoteJobsOnly { get; set; }

    public string[] Keywords { get; set; } = [];
    public string[] AvoidJobKeywords { get; set; } = [];
}