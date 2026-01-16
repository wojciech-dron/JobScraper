namespace JobScraper.Web.Features.JobOffers.Scrape;

public static class Setup
{
    public static WebApplicationBuilder AddScrapeServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<ScrapeHandler>();

        return builder;
    }
}
