using Microsoft.Extensions.DependencyInjection;
using PgQueue.Abstractions;
using System.Text.Json;

namespace PgQueue.Core.Dispatch;

internal class JobDispatcher
{
    private readonly IServiceProvider _services;
    private readonly IJobHandlerRegistry _registry;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public JobDispatcher(
        IServiceProvider services,
        IJobHandlerRegistry registry)
    {
        _services = services;
        _registry = registry;
    }

    public async Task DispatchAsync(
        DequeuedJob job,
        CancellationToken cancellationToken)
    {
        var descriptor = _registry.Get(job.JobType);

        var payload = JsonSerializer.Deserialize(
                job.Payload,
                descriptor.PayloadType,
                JsonOptions);

        if (payload is null)
        {
            throw new InvalidOperationException($"Could not deserialize payload for job '{job.JobType}'.");
        }

        var handler = _services.GetRequiredService(descriptor.HandlerType);

        var method = descriptor.HandlerType.GetMethod("HandleAsync");

        if (method is null)
        {
            throw new InvalidOperationException( $"Handler '{descriptor.HandlerType.Name}' " +
                "does not contain HandleAsync.");
        }

        var context = new JobContext(
            job.Id,
            job.JobType,
            job.Attempts + 1,
            job.CreatedAt);

        var task = (Task?)method.Invoke(handler,
                [
                    payload,
                    context,
                    cancellationToken
                ]);

        if (task is null)
            throw new InvalidOperationException(
                "Handler returned null Task.");

        await task;
    }
}
