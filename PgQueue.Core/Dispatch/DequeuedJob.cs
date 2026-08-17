namespace PgQueue.Core.Dispatch;

internal sealed record DequeuedJob(
    long Id,
    string JobType,
    string Payload,
    int Attempts,
    int MaxAttempts,
    DateTimeOffset CreatedAt);
