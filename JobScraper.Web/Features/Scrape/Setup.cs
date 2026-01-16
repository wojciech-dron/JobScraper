namespace JobScraper.Web.Features.Scrape;

public static class Setup
{
    public static WebApplicationBuilder AddScrapeServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<ScrapeHandler>();

        return builder;
    }
}
