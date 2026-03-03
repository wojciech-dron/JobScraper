namespace JobScraper.Web.Features.Cv;

public static class Setup
{
    public static IEndpointRouteBuilder UseCvFeatures(this IEndpointRouteBuilder app)
    {
        app.MapCvImageEndpoints();

        return app;
    }
}
