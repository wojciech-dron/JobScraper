using JobScraper.IntegrationTests.Host.HttpMocks;
using JobScraper.IntegrationTests.Host.Persistence;
using JobScraper.IntegrationTests.Host.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JobScraper.IntegrationTests.Host;

/// <summary> Host builder (services, DI and configuration) for integration tests </summary>
internal sealed class WebApiTestFactory(string connectionString) : WebApplicationFactory<Program>
{
    private readonly TestLoggerProvider testLoggerProvider = new();

    public ITestOutputHelper? TestOutput
    {
        set => testLoggerProvider.TestOutput = value;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.SetMinimumLevel(LogLevel.Error);
            loggingBuilder.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Information); // sql queries from ef in test output
            loggingBuilder.AddProvider(testLoggerProvider);
        });

        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString,
                ["AppSettings:TickerQ:DisableJobs"] = "true",
            });
        });

        builder.ConfigureServices(s =>
        {
            s.AddTestPersistence(connectionString);
            s.MockAllHttpClients();
            s.AddTimeProviderMock();
            s.AddObjectMother();

            s.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName, _ => { });
        });
    }
}
