namespace PgQueue.Core.Dispatch;

public interface IConfigureJobHandlerRegistry
{
    void Apply(JobHandlerRegistry registry);
}
