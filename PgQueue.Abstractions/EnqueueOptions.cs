namespace PgQueue.Abstractions;

public record EnqueueOptions
{
    public string? JobKey { get; init; }

    public int MaxAttempts { get; init; } = 5;

    public TimeSpan? Delay { get; init; }
}