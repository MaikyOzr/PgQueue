using Microsoft.Extensions.Logging;

namespace PgQueue.Abstractions;

public class TestJobHandler : IJobHandler<TestJobPayload>
{
    private readonly ILogger<TestJobHandler> _logger;

    public TestJobHandler(ILogger<TestJobHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(TestJobPayload payload, JobContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "PgQueue TEST JOB EXECUTED: JobId={JobId}, Message={Message}, Attempt={Attempt}",
            context.JobId,
            payload.Message,
            context.AttemptNumber);

        await Task.CompletedTask;
    }
}
