using PgQueue.Abstractions;
using PgQueue.Core.Dispatch;
using Xunit;

namespace PgQueue.Testing.Tests;

public class JobHandlerRegistryTests
{
    [Fact]
    public void Register_ValidHandler_SetsInRegistry()
    {
        var registry = new JobHandlerRegistry(Enumerable.Empty<IConfigureJobHandlerRegistry>());

        registry.Register<TestJobHandler, TestJobPayload>("test-job-type");

        var descriptor = registry.Get("test-job-type");
        Assert.Equal(typeof(TestJobHandler), descriptor.HandlerType);
        Assert.Equal(typeof(TestJobPayload), descriptor.PayloadType);
    }

    [Fact]
    public void Get_RegisteredHandler_ReturnsDescriptor()
    {
        var configurations = new IConfigureJobHandlerRegistry[]
        {
            new ConfigureJobHandlerRegistry<TestJobHandler, TestJobPayload>("test-job-type")
        };
        var registry = new JobHandlerRegistry(configurations);

        var descriptor = registry.Get("test-job-type");

        Assert.NotNull(descriptor);
        Assert.Equal(typeof(TestJobHandler), descriptor.HandlerType);
    }

    [Fact]
    public void Get_UnregisteredType_ThrowsJobHandlerNotFoundException()
    {
        var registry = new JobHandlerRegistry(Enumerable.Empty<IConfigureJobHandlerRegistry>());

        var exception = Assert.Throws<JobHandlerNotFoundException>(() => registry.Get("nonexistent-job-type"));
        Assert.Equal("nonexistent-job-type", exception.JobType);
    }

    [Fact]
    public void Register_MultipleHandlers_AllRetrievable()
    {
        var configurations = new IConfigureJobHandlerRegistry[]
        {
            new ConfigureJobHandlerRegistry<TestJobHandler, TestJobPayload>("job-type-1"),
            new ConfigureJobHandlerRegistry<TestJobHandler, TestJobPayload>("job-type-2")
        };
        var registry = new JobHandlerRegistry(configurations);

        var desc1 = registry.Get("job-type-1");
        var desc2 = registry.Get("job-type-2");

        Assert.Equal(typeof(TestJobHandler), desc1.HandlerType);
        Assert.Equal(typeof(TestJobHandler), desc2.HandlerType);
    }
}