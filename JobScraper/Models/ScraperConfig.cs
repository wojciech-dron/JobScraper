namespace JobScraper.Models;

public class ScraperConfig
{
    public string SearchTerm { get; set; } = ".Net";
    public string Location { get; set; } = "";
    public int ListingAgeInDays { get; set; } = 3;
    public int WaitForListSeconds { get; set; } = 8;
    public int WaitForDetailsSeconds { get; set; } = 5;
    public bool RemoteJobsOnly { get; set; }

    public string[] Keywords { get; set; } =
    [
        "c#", ".net", "sql", "blazor", "razor", "asp.net", "ef core", "entity framework", "jr", "junior",
        "typescript", "javascript", "angular", "git", "html", "css", "tailwind", "material", "bootstrap", "ai"
    ];

    public string[] AvoidJobKeywords { get; set; } = ["lead", "junior", "manager"];
}