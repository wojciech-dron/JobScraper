using Testcontainers.PostgreSql;

namespace JobScraper.IntegrationTests.Host.Persistence;

internal sealed class PostgresContainer : IAsyncDisposable
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:18.1-bookworm")
        .WithName("shitty-analyzer-tests-postgres")
        .WithDatabase("shitty-analyzer-tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .WithPortBinding(5433, 5432)
        .WithReuse(reuseEnabled)
        .Build();

    /// <summary>
    ///     A flag indicating whether the reuse of the Postgres container
    ///     is enabled across test runs. When enabled, the container is not
    ///     disposed of after test execution.
    ///     The value is determined by the environment variable "TESTCONTAINERS_REUSE_ENABLE"
    /// </summary>
    /// <remarks>
    ///     If reuse is enabled and there are database errors (eg. migration change),
    ///     it is required to manually delete the container.
    /// </remarks>
    private static readonly bool reuseEnabled =
        Environment.GetEnvironmentVariable("TESTCONTAINERS_REUSE_ENABLE") == "true";

    public string ConnectionString => container.GetConnectionString();

    public async ValueTask InitializeAsync() =>
        await container.StartAsync(TestContext.Current.CancellationToken);

    public async ValueTask DisposeAsync()
    {
        if (!reuseEnabled)
            await container.DisposeAsync();
    }
}
