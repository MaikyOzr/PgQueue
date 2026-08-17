namespace PgQueue.Core.Dispatch;

internal interface IJobHandlerRegistry
{
    JobHandlerDescriptor Get(string jobType);
}
