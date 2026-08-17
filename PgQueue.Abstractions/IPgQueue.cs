namespace PgQueue.Abstractions;

public interface IPgQueue
{
    Task<long> EnqueueAsync<TPayload>(string jobType, 
        TPayload payload, 
        EnqueueOptions? options, 
        CancellationToken cancellationToken = default);
}
