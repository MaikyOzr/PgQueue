using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using PgQueue.Core.Dispatch;
using PgQueue.Core.Internal;
using PgQueue.Core.Retry;
using System.Data.Common;

namespace PgQueue.Core.Worker;

internal sealed class PgQueueBackgroundService : BackgroundService
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly JobDispatcher _dispatcher;
    private readonly PgQueueWorkerOptions _options;
    private readonly ILogger<PgQueueBackgroundService> _logger;

    public PgQueueBackgroundService(
        NpgsqlDataSource dataSource,
        JobDispatcher dispatcher,
        PgQueueWorkerOptions options,
        ILogger<PgQueueBackgroundService> logger)
    {
        _dataSource = dataSource;
        _dispatcher = dispatcher;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        // WorkerCount creates concurrent job processing workers
        // Each worker has its own ListenNotifyClient for LISTEN/NOTIFY
        var tasks = Enumerable.Range(
            0,
            _options.WorkerCount)
            .Select(_ => WorkerLoopAsync(stoppingToken));

        await Task.WhenAll(tasks);
    }

    private async Task WorkerLoopAsync(
        CancellationToken cancellationToken)
    {
        var workerId = Guid.NewGuid();

        await using var listener =
            new ListenNotifyClient(
                _dataSource,
                _options.NotificationChannel);

        await listener.StartAsync(
            cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            // Check for crash recovery - reset jobs that have been processing too long
            await ResetTimedOutJobsAsync(cancellationToken);

            // Try dequeue immediately (atomic via SKIP LOCKED)
            DequeuedJob? job = null;

            try
            {
                job = await DequeueAsync(
                    workerId,
                    cancellationToken);

                if (job is null)
                {
                    // No job available - wait for LISTEN/NOTIFY
                    await listener.WaitAsync(
                        _options.PollingFallbackInterval,
                        cancellationToken);

                    continue;
                }

                await ProcessAsync(
                    job,
                    cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected PgQueue worker error processing job.");
            }
        }
    }

    private async Task ResetTimedOutJobsAsync(CancellationToken cancellationToken)
    {
        await using var connection =
            await _dataSource.OpenConnectionAsync(cancellationToken);

        await using var command =
            connection.CreateCommand();

        command.CommandText = @"
            UPDATE pgqueue_jobs
            SET
                status = 0,
                available_at = now() - @timeout,
                locked_by = NULL,
                locked_at = NULL
            WHERE status = 1
              AND locked_at < now() - @timeout;

            SELECT COUNT(*) as ResetCount FROM pgqueue_jobs;";

        command.Parameters.Add(
            new NpgsqlParameter(
                "@timeout",
                _options.ProcessingTimeout));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<DequeuedJob?> DequeueAsync(
        Guid workerId,
        CancellationToken cancellationToken)
    {
        await using var connection =
            await _dataSource.OpenConnectionAsync(
                cancellationToken);

        await using var command =
            connection.CreateCommand();

        command.CommandText =
            PgQueueSql.Dequeue;

        AddParameter(
            command,
            "workerId",
            workerId);

        await using var reader =
            await command.ExecuteReaderAsync(
                cancellationToken);

        if (!await reader.ReadAsync(
                cancellationToken))
        {
            return null;
        }

        return new DequeuedJob(
            Id: reader.GetInt64(0),
            JobType: reader.GetString(1),
            Payload: reader.GetString(2),
            Attempts: reader.GetInt32(3),
            MaxAttempts: reader.GetInt32(4),
            CreatedAt: reader.GetFieldValue<DateTimeOffset>(5));
    }

    private async Task ProcessAsync(
        DequeuedJob job,
        CancellationToken cancellationToken)
    {
        try
        {
            await _dispatcher.DispatchAsync(
                job,
                cancellationToken);

            await CompleteAsync(
                job.Id,
                cancellationToken);
        }
        catch (Exception ex)
        {
            await FailAsync(
                job,
                ex,
                cancellationToken);
        }
    }

    private async Task CompleteAsync(
        long jobId,
        CancellationToken cancellationToken)
    {
        await using var connection =
            await _dataSource.OpenConnectionAsync(
                cancellationToken);

        await using var command =
            connection.CreateCommand();

        command.CommandText =
            PgQueueSql.CompleteJob;

        AddParameter(
            command,
            "id",
            jobId);

        await command.ExecuteNonQueryAsync(
            cancellationToken);
    }

    private async Task FailAsync(
        DequeuedJob job,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var nextAttempt =
            job.Attempts + 1;

        await using var connection =
            await _dataSource.OpenConnectionAsync(
                cancellationToken);

        await using var command =
            connection.CreateCommand();

        if (nextAttempt >= job.MaxAttempts)
        {
            command.CommandText =
                PgQueueSql.FailJobDead;

            AddParameter(
                command,
                "error",
                exception.ToString());
        }
        else
        {
            command.CommandText =
                PgQueueSql.FailJobRetry;

            var delay =
                ExponentialBackoffPolicy.Calculate(
                    nextAttempt);

            AddParameter(
                command,
                "availableAt",
                DateTimeOffset.UtcNow + delay);

            AddParameter(
                command,
                "error",
                exception.ToString());
        }

        AddParameter(
            command,
            "id",
            job.Id);

        await command.ExecuteNonQueryAsync(
            cancellationToken);
    }

    private static void AddParameter(
        DbCommand command,
        string name,
        object value)
    {
        var parameter =
            command.CreateParameter();

        parameter.ParameterName = name;
        parameter.Value = value;

        command.Parameters.Add(parameter);
    }
}