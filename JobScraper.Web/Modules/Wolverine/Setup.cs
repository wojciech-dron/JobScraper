using Wolverine;

namespace JobScraper.Web.Modules.Wolverine;

public static class Setup
{
    /// <remarks> Must be here because of source generation </remarks>
    public static WebApplicationBuilder AddWolverineFxModule(this WebApplicationBuilder builder)
    {
        builder.Host.UseWolverine(opts =>
            { },
            ExtensionDiscovery.ManualOnly);

        return builder;
    }
}
