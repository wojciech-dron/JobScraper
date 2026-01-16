using System.Reflection;
using System.Runtime.Versioning;
using JobScraper.Web.Common.Entities;

namespace JobScraper.Web.Modules.Settings;

public static class Setup
{
    public static WebApplicationBuilder AddScraperSettings(this WebApplicationBuilder builder)
    {
        var environment = builder.Environment;
        var configuration = builder.Configuration;

        var appPath = Path.Combine(environment.ContentRootPath);

        if (Directory.Exists(Path.Combine(appPath, "bin")))
            appPath = Path.Combine(appPath, "bin", GetBuildConfig(), GetTargetFramework());

        configuration.AddJsonFile(Path.Combine(appPath, "scraperSettings.json"), true, true);
        configuration.AddJsonFile(Path.Combine(appPath, $"scraperSettings.{environment.EnvironmentName}.json"), true, true);

        configuration.AddEnvironmentVariables();

        builder.Services.Configure<AppSettings>(configuration.GetSection(AppSettings.SectionName));


        return builder;
    }

    private static string GetBuildConfig()
    {
#if DEBUG
        return "Debug";
#else
            return "Release";
#endif
    }

    private static string GetTargetFramework()
    {
        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly == null)
            throw new InvalidOperationException("Entry assembly not found.");

        if (entryAssembly
                .GetCustomAttributes(typeof(TargetFrameworkAttribute), false)
                .FirstOrDefault() is TargetFrameworkAttribute targetFrameworkAttribute)
            return "net" + targetFrameworkAttribute.FrameworkName.Split("=v").Last();

        throw new InvalidOperationException("Target framework attribute not found.");
    }
}
