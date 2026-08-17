namespace PgQueue.Abstractions;

public sealed class DuplicateJobKeyException : Exception
{
    public DuplicateJobKeyException(string jobKey)
        : base($"Job with key '{jobKey}' already exists.")
    {
        JobKey = jobKey;
    }

    public string JobKey { get; }
}

public sealed class JobHandlerNotFoundException : Exception
{
    public JobHandlerNotFoundException(string jobType)
        : base($"No handler registered for job type '{jobType}'.")
    {
        JobType = jobType;
    }

    public string JobType { get; }
}
