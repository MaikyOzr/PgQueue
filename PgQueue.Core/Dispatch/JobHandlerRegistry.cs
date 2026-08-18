using PgQueue.Abstractions;

namespace PgQueue.Core.Dispatch;

public class JobHandlerRegistry : IJobHandlerRegistry
{
    private readonly Dictionary<string, JobHandlerDescriptor> _handlers = new(StringComparer.Ordinal);

    public JobHandlerRegistry(IEnumerable<IConfigureJobHandlerRegistry> configurations)
    {
        foreach (var configure in configurations)
        {
            configure.Apply(this);
        }
    }

    public void Register<THandler, TPayload>(string jobType)
        where THandler : class, IJobHandler<TPayload>
    {
        _handlers[jobType] = new JobHandlerDescriptor(typeof(THandler), typeof(TPayload));
    }

    public JobHandlerDescriptor Get(string jobType)
    {
        if (_handlers.TryGetValue(jobType, out var descriptor))
        {
            return descriptor;
        }

        throw new JobHandlerNotFoundException(jobType);
    }
}
