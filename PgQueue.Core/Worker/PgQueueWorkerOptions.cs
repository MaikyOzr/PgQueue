namespace PgQueue.Core.Worker;

public sealed class PgQueueWorkerOptions
{
    public int WorkerCount { get; set; } = 4;

    public TimeSpan PollingFallbackInterval { get; set; } = TimeSpan.FromSeconds(30);

    public string NotificationChannel { get; set; } = "pgqueue_new_job";

    public TimeSpan ProcessingTimeout { get; set; } = TimeSpan.FromMinutes(5);
}