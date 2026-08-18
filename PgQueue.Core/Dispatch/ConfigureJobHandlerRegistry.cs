using PgQueue.Abstractions;

namespace PgQueue.Core.Dispatch;

public sealed class ConfigureJobHandlerRegistry<THandler, TPayload> : IConfigureJobHandlerRegistry
where THandler : class, IJobHandler<TPayload>
{
    private readonly string _jobType;

    public ConfigureJobHandlerRegistry(string jobType) => _jobType = jobType;

    public void Apply(JobHandlerRegistry registry) => registry.Register<THandler, TPayload>(_jobType);
}
