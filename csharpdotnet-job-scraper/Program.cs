﻿using Microsoft.Playwright;
using indeed_scraper;

const string jobSearchTerm = "C#";
const string location = "Cuyahoga Falls, OH";
const int radius = 50;
const int secondsToWait = 10;
const int experience = 2; // 1 - Internship, 2 - Entry Level, 3 - Associate, 4 - Mid-Senior, 5 - Senior, 6 - Executive
const int listingAge = 86400; // 3600 - 1 hour, 86400 - 1 day, 1 week - 604800, 2 weeks - 1209600, 30 days - 2592000
string[] keywords =
{
    "c#", ".net", "sql", "blazor", "razor", "asp.net", "ef core", "entity framework", "typescript", "javascript", 
    "angular", "git", "html", "css", "tailwind", "material", "bootstrap"
};
string[] avoidJobKeywords =
{
    "lead", "senior"
};

// added this back to avoid strange results returned
var encodedJobSearchTerm = System.Web.HttpUtility.UrlEncode(jobSearchTerm);
var encodedLocation = System.Web.HttpUtility.UrlEncode(location);

var indeedUrl = $"https://www.indeed.com/jobs?q={encodedJobSearchTerm}&l={encodedLocation}&radius={radius}";
var linkedinUrl = $"https://www.linkedin.com/jobs/search/?distance={radius}&f_E={experience}&f_TPR=r{listingAge}&keywords={encodedJobSearchTerm}&location={encodedLocation}";

using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Firefox.LaunchAsync();
var context = await browser.NewContextAsync(new BrowserNewContextOptions
{
    UserAgent = "Mozilla/5.0 (Windows NT 10.0; rv:114.0) Gecko/20100101 Firefox/114.0"
});
var indeedPage = await context.NewPageAsync();
await indeedPage.GotoAsync(indeedUrl);
await indeedPage.WaitForTimeoutAsync(secondsToWait * 1000);
var linkedinPage = await context.NewPageAsync();
await linkedinPage.GotoAsync(linkedinUrl);
await linkedinPage.WaitForTimeoutAsync(secondsToWait * 1000);

using (var db = new JobDbContext())
{
    db.Database.EnsureCreated();
    
    var pageCount = 0;
    var hasNextPage = true;

    while (hasNextPage)
    {
        pageCount++;
        Console.WriteLine($"Indeed scraping page {pageCount}...");

        var indeedTitleElements = await indeedPage.QuerySelectorAllAsync("h2.jobTitle");
        var titles = await Task.WhenAll(indeedTitleElements.Select(async t => await t.InnerTextAsync()));

        var indeedCompanyElements = await indeedPage.QuerySelectorAllAsync("span.companyName");
        var companyNames = await Task.WhenAll(indeedCompanyElements.Select(async c => await c.InnerTextAsync()));

        var indeedLocationElements = await indeedPage.QuerySelectorAllAsync("div.companyLocation");
        var locations = await Task.WhenAll(indeedLocationElements.Select(async l => (await l.InnerTextAsync()).Trim()));

        for (var i = 0; i < titles.Length; i++)
        {
            await indeedTitleElements[i].ClickAsync();
            await indeedPage.WaitForTimeoutAsync(secondsToWait * 1000);

            var indeedJobDescriptionElement = await indeedPage.QuerySelectorAsync("#jobDescriptionText");
            var indeedDescription = await indeedJobDescriptionElement.InnerTextAsync();

            var indeedApplyUrlElement = await indeedPage.QuerySelectorAsync("span[data-indeed-apply-joburl], " +
                                                                    "button[href*='https://www.indeed.com/applystart?jk=']");
            string indeedApplyUrl = null;
            if (indeedApplyUrlElement != null)
            {
                var indeedApplyJobUrl = await indeedApplyUrlElement.GetAttributeAsync("data-indeed-apply-joburl");
                var href = await indeedApplyUrlElement.GetAttributeAsync("href");

                if (!string.IsNullOrEmpty(indeedApplyJobUrl))
                    indeedApplyUrl = indeedApplyJobUrl;
                else if (!string.IsNullOrEmpty(href))
                    indeedApplyUrl = href.Split('&')[0];
            }

            var foundKeywordsForJob = keywords
                .Where(keyword => indeedDescription.Contains(keyword, StringComparison.OrdinalIgnoreCase)).ToList();

            if (avoidJobKeywords.Any(uk => titles[i].Contains(uk, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }
            
            if (foundKeywordsForJob.Any())
            {
                var job = new Job
                {
                    Origin = "Indeed",
                    SearchTerm = jobSearchTerm,
                    Title = titles[i],
                    CompanyName = companyNames[i],
                    Location = locations[i],
                    Description = indeedDescription,
                    FoundKeywords = string.Join(",", foundKeywordsForJob),
                    ApplyUrl = indeedApplyUrl,
                    ScrapedAt = DateTime.Now,
                };

                db.Jobs.Add(job);
            }
        }

        await db.SaveChangesAsync();
        Console.WriteLine($"Page {pageCount} complete.");

        var nextButton = await indeedPage.QuerySelectorAsync("a[data-testid='pagination-page-next']");
        if (nextButton != null)
        {
            await nextButton.ClickAsync();
            await indeedPage.WaitForTimeoutAsync(secondsToWait * 1000);
        }
        else
        {
            hasNextPage = false;
        }
    }

    Console.WriteLine("Indeed scraping complete.");

    // Begin LinkedIn scraping

    var linkedinTitleElements = await linkedinPage.QuerySelectorAllAsync("span.sr-only");
    var linkedinTitles = await Task.WhenAll(linkedinTitleElements.Select(async t => await t.InnerTextAsync()));

    var linkedinCompanyElements = await linkedinPage.QuerySelectorAllAsync("a.hidden-nested-link");
    var linkedinCompanyNames = await Task.WhenAll(linkedinCompanyElements.Select(async c => await c.InnerTextAsync()));

    var linkedinLocationElements = await linkedinPage.QuerySelectorAllAsync("span.job-search-card__location");
    var linkedinLocations = await Task.WhenAll(linkedinLocationElements.Select(async l => (await l.InnerTextAsync()).Trim()));

    var linkedinApplyElements = await linkedinPage.QuerySelectorAllAsync("a.job-card-list__title");
    var linkedinApplyUrls = await Task.WhenAll(linkedinApplyElements.Select(async l => await l.GetAttributeAsync("href")));

    for (var i = 0; i < linkedinTitles.Length; i++)
    {
        await linkedinPage.GotoAsync(linkedinApplyUrls[i]);
        await linkedinPage.WaitForTimeoutAsync(secondsToWait * 1000);

        var linkedinJobDescriptionElement = await linkedinPage.QuerySelectorAsync(".description__text");
        var linkedinDescription = linkedinJobDescriptionElement != null ? await linkedinJobDescriptionElement.InnerTextAsync() : "";

        var foundKeywordsForJob = keywords
            .Where(keyword => linkedinDescription.Contains(keyword, StringComparison.OrdinalIgnoreCase)).ToList();

        if (avoidJobKeywords.Any(uk => linkedinTitles[i].Contains(uk, StringComparison.OrdinalIgnoreCase)))
        {
            continue;
        }

        // Only save the job if there are matching keywords in the description
        if (foundKeywordsForJob.Any())
        {
            var job = new Job
            {
                Origin = "LinkedIn",
                SearchTerm = jobSearchTerm,
                Title = linkedinTitles[i],
                CompanyName = linkedinCompanyNames[i],
                Location = linkedinLocations[i],
                Description = linkedinDescription,
                FoundKeywords = string.Join(",", foundKeywordsForJob),
                ApplyUrl = linkedinApplyUrls[i],
                ScrapedAt = DateTime.Now,
            };

            db.Jobs.Add(job);
        }
    }

    await db.SaveChangesAsync();

    Console.WriteLine("LinkedIn scraping complete.");
}
