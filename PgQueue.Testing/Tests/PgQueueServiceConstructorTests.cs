using Npgsql;
using PgQueue.Abstractions;
using PgQueue.Core;
using Xunit;

namespace PgQueue.Testing.Tests;

public class PgQueueServiceConstructorTests
{
    [Fact]
    public void Constructor_WithNullTransactionAccessor_DoesNotThrow()
    {
        var dataSource = NpgsqlDataSource.Create(
            "Host=localhost;Port=59999;Database=nonexistent;Username=x;Password=x");

        var service = new PgQueueService(dataSource, transactionAccessor: null);

        Assert.NotNull(service);
    }

    [Fact]
    public async Task Constructor_WithNullTransactionAccessor_FallsBackToOwnConnection_NotNullReferenceException()
    {
        var dataSource = NpgsqlDataSource.Create(
            "Host=localhost;Port=59999;Database=nonexistent;Username=x;Password=x");
        var service = new PgQueueService(dataSource, transactionAccessor: null);

        await Assert.ThrowsAsync<NpgsqlException>(async () =>
            await service.EnqueueAsync("test-job-type", new TestJobPayload("test"), options: null));
    }
}