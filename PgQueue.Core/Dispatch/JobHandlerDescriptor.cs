namespace PgQueue.Core.Dispatch;

internal sealed record JobHandlerDescriptor(Type HandlerType, Type PayloadType);