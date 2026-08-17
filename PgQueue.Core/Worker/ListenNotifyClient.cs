using Npgsql;

namespace PgQueue.Core.Worker;

internal sealed class ListenNotifyClient
    : IAsyncDisposable
{
    private readonly NpgsqlConnection _connection;
    public string Channel { get; }

    public event Action? NotificationReceived;

    public ListenNotifyClient(
        NpgsqlDataSource dataSource,
        string channel)
    {
        _connection =
            dataSource.CreateConnection();

        Channel = channel;
    }

    public async Task StartAsync(
        CancellationToken cancellationToken)
    {
        await _connection.OpenAsync(cancellationToken);

        _connection.Notification += OnNotification;

        await using var command = _connection.CreateCommand();

        command.CommandText = $"LISTEN {QuoteIdentifier(Channel)}";

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private void OnNotification(
        object sender,
        NpgsqlNotificationEventArgs e)
    {
        NotificationReceived?.Invoke();
    }

    public async Task WaitAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        timeoutCts.CancelAfter(timeout);

        try
        {
            await _connection.WaitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            // Normal polling timeout.
        }
    }

    private static string QuoteIdentifier(
        string identifier)
    {
        return "\"" +
            identifier.Replace(
                "\"",
                "\"\"") +
            "\"";
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}
