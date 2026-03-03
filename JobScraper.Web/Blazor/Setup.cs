using System.Globalization;
using JobScraper.Web.Features.Account;
using JobScraper.Web.Features.Cv;

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
        // app.MapStaticAssets(); // this causes 405 for POST on tickerq dashboard, but it might breaks somthing else

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

        app.MapAdditionalIdentityEndpoints();

        return app;
    }
}
