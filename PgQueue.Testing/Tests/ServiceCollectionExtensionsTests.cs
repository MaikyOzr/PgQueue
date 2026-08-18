using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PgQueue.Abstractions;
using PgQueue.Core;
using PgQueue.Core.Dispatch;
using PgQueue.EntityFrameworkCore;
using Xunit;

namespace PgQueue.Testing.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddJobHandler_RegistersHandlerInRegistry_ViaFullDiPipeline()
    {
        var services = new ServiceCollection();

        services.AddPgQueue(options =>
        {
            options.ConnectionString = "Host=localhost;Port=59999;Database=x;Username=x;Password=x";
        });

        services.AddJobHandler<TestJobHandler, TestJobPayload>("test-job-type");

        var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IJobHandlerRegistry>();

        // Це той самий виклик, який раніше кидав JobHandlerNotFoundException
        // через зламану DI-реєстрацію — тепер має повернути дескриптор.
        var descriptor = registry.Get("test-job-type");

        Assert.Equal(typeof(TestJobHandler), descriptor.HandlerType);
        Assert.Equal(typeof(TestJobPayload), descriptor.PayloadType);
    }

    [Fact]
    public void AddPgQueueEntityFrameworkCore_RegistersTransactionAccessor()
    {
        var services = new ServiceCollection();

        services.AddPgQueue(options =>
        {
            options.ConnectionString = "Host=localhost;Port=59999;Database=x;Username=x;Password=x";
        });

        services.AddDbContext<AppDbContext>(o => o.UseNpgsql("Host=localhost;Port=59999;Database=x"));
        services.AddPgQueueEntityFrameworkCore<AppDbContext>();

        var provider = services.BuildServiceProvider();

        // Раніше падало тут — сервіс просто не був зареєстрований.
        var accessor = provider.GetService<PgQueue.Core.Internal.IPgQueueTransactionAccessor>();

        Assert.NotNull(accessor);
    }
}