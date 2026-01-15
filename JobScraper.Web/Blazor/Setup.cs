using System.Globalization;

namespace JobScraper.Web.Blazor;

public static class Setup
{
    public static WebApplicationBuilder AddBlazor(this WebApplicationBuilder builder)
    {
        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        builder.Services.AddQuickGridEntityFrameworkAdapter();
        builder.Services.AddBlazorBootstrap();
        builder.WebHost.UseStaticWebAssets();
        builder.Services.AddHttpContextAccessor();

        return builder;
    }

    public static WebApplication UseBlazor(this WebApplication app)
    {
        app.UseAntiforgery();
        app.UseStaticFiles();
        app.MapStaticAssets();

        var supportedCultures = new[]
        {
            new CultureInfo("pl-PL"),
        };
        app.UseRequestLocalization(new RequestLocalizationOptions
        {
            SupportedCultures = supportedCultures,
            SupportedUICultures = supportedCultures,
        });

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        return app;
    }
}
