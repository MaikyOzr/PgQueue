namespace PgQueue.Core.Dispatch;

public interface IJobHandlerRegistry
{
    JobHandlerDescriptor Get(string jobType);
}
