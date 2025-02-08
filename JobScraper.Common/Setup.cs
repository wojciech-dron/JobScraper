﻿using System.Reflection;
using System.Runtime.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace JobScraper.Common;

public static class Setup
{
    public static IConfigurationManager AddScrapperSettings(this IConfigurationManager configuration,
        IHostEnvironment environment)
    {
        var appPath = Path.Combine(environment.ContentRootPath, "bin", GetBuildConfig(), GetTargetFramework());
        configuration.AddJsonFile(Path.Combine(appPath, "scraperSettings.json"), optional: false, reloadOnChange: true);
        configuration.AddJsonFile(Path.Combine(appPath, $"scraperSettings.{environment.EnvironmentName}.json"), optional: true, reloadOnChange: true);

        return configuration;
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
        {
            return "net" + targetFrameworkAttribute.FrameworkName[^3..];
        }

        throw new InvalidOperationException("Target framework attribute not found.");
    }
}