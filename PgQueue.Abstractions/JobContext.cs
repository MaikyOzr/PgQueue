namespace PgQueue.Abstractions;

public sealed record JobContext(long JobId,
    string JobType, 
    int AttemptNumber, 
    DateTimeOffset EnqueuedAt);