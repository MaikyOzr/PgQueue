namespace PgQueue.Core;

public sealed class PgQueueOptions
{
    public required string ConnectionString { get; set; }

    public int WorkerCount { get; set; } = 4;

    public TimeSpan PollingFallbackInterval { get; set; } = TimeSpan.FromSeconds(30);

    public string NotificationChannel { get; set; } = "pgqueue_new_job";
}
