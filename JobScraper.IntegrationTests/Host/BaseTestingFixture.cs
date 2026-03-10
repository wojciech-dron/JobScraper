using JobScraper.IntegrationTests.Host.Persistence;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;

namespace JobScraper.IntegrationTests.Host;

/// <summary>
///     Initializes and resets the database before and after each test. Shared across all integration tests.
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class BaseTestingFixture : IAsyncLifetime
{
    private WebApiTestFactory factory = null!;
    private IServiceScopeFactory scopeFactory = null!;
    private PostgresContainer? dbContainer;
    private DbRespawner respawner = null!;

    public Lazy<HttpClient> AnonymousClient => new(factory.CreateClient());

    public ITestOutputHelper? TestOutput
    {
        set => factory.TestOutput = value;
    }

    /// <summary> Global setup for tests </summary>
    public async ValueTask InitializeAsync()
    {
        // var testConnectionString = Environment.GetEnvironmentVariable("CONNECTIONSTRINGS__TESTPOSTGRES"); // postgres
        var testConnectionString = "Data Source=JobScraperTest;Mode=Memory;Cache=Shared"; // sqlite in memory
        if (string.IsNullOrEmpty(testConnectionString))
        {
            dbContainer = new PostgresContainer();
            await dbContainer.InitializeAsync();
            testConnectionString = dbContainer.ConnectionString;
        }

        factory = new WebApiTestFactory(testConnectionString);
        scopeFactory = factory.Services.GetRequiredService<IServiceScopeFactory>();

        respawner = new DbRespawner(testConnectionString);
        await respawner.InitializeAsync();
    }

    /// <summary> Global cleanup for tests </summary>
    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        await factory.DisposeAsync();
        respawner.Dispose();

        if (dbContainer is not null)
            await dbContainer.DisposeAsync();
    }

    /// <summary> Setup for each test </summary>
    public virtual async Task TestSetup()
    {
        var mockHttpHandler = factory.Services.GetService<MockHttpMessageHandler>();
        mockHttpHandler?.Clear(); // clear mock setup
        await respawner.ResetDbAsync();
    }

    public IServiceScope CreateScope() => scopeFactory.CreateScope();
}

[CollectionDefinition]
public class TestingDatabaseFixtureCol : ICollectionFixture<BaseTestingFixture>, ICollectionFixture<PostgresContainer>;
