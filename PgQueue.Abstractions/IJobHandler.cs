namespace PgQueue.Abstractions;

public interface IJobHandler<TPayload>
{
    Task HandleAsync(TPayload payload, JobContext context, CancellationToken cancellationToken = default);
}
