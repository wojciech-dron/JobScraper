using Wolverine;

namespace JobScraper.Modules.Wolverine;

public static class Setup
{
    public static WebApplicationBuilder AddWolverineServices(this WebApplicationBuilder builder)
    {
        builder.Host.UseWolverine(o =>
        {
            o.ApplicationAssembly = typeof(Setup).Assembly;
        });

        return builder;
    }
}
