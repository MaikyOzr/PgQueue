using Microsoft.Extensions.Hosting;
using Npgsql;
using PgQueue.Core.Schema;

/// <summary>
/// Runs once at host startup (not at DI registration time) to ensure the
/// pgqueue_jobs schema exists. Registered before PgQueueBackgroundService so
/// hosted services execute in order and the table exists before any worker
/// tries to dequeue from it.
/// </summary>
public sealed class PgQueueSchemaInitializer : IHostedService
{
    private readonly NpgsqlDataSource _dataSource;

    public PgQueueSchemaInitializer(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = PgQueueSchemaSql.EnsureCreated;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
