using System.Data.Common;
using Microsoft.Data.Sqlite;
using Respawn;

namespace JobScraper.IntegrationTests.Host.Persistence;

internal sealed class DbRespawner(string connectionString) : IDisposable
{
    private DbConnection? connection;
    private Respawner? respawner;

    public async Task InitializeAsync()
    {
        connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        respawner = await Respawner.CreateAsync(connection,
            new RespawnerOptions
            {
                DbAdapter = DbAdapter.Sqlite,
            });
    }

    public async Task ResetDbAsync()
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(respawner);

        await respawner.ResetAsync(connection);
    }

    public void Dispose() => connection?.Dispose();
}
