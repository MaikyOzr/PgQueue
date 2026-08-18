namespace PgQueue.Core.Dispatch;

public sealed record JobHandlerDescriptor(Type HandlerType, Type PayloadType);