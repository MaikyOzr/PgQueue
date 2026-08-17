using System;
using Xunit;
using PgQueue.Abstractions;

namespace PgQueue.Testing.Tests;

public class PublicApiTests
{
    [Fact]
    public void TestEnqueueOptionsDefaults()
    {
        var options = new EnqueueOptions();
        Assert.Equal(5, options.MaxAttempts);
        Assert.Null(options.Delay);
        Assert.Null(options.JobKey);
    }

    [Fact]
    public void TestJobPayloadCreation()
    {
        var payload = new TestJobPayload("test-message");
        Assert.Equal("test-message", payload.Message);
    }

    [Fact]
    public void TestJobContextCreation()
    {
        var context = new JobContext(1, "test-type", 1, DateTimeOffset.UtcNow);
        Assert.Equal(1, context.JobId);
        Assert.Equal("test-type", context.JobType);
        Assert.Equal(1, context.AttemptNumber);
    }
}
