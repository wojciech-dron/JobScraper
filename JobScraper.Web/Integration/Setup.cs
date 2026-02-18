namespace JobScraper.Web.Integration;

public static class Setup
{
    public static WebApplicationBuilder AddIntegrationServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddHttpClient();

        return builder;
    }
}
