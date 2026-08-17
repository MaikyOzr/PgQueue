using Npgsql;
using PgQueue.Abstractions;
using PgQueue.Core.Internal;
using System.Data.Common;
using System.Text.Json;

namespace PgQueue.Core;

public sealed class PgQueueService : IPgQueue
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly IPgQueueTransactionAccessor? _transactionAccessor;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public PgQueueService(
        NpgsqlDataSource dataSource,
        IPgQueueTransactionAccessor? transactionAccessor = null)
    {
        _dataSource = dataSource;
        _transactionAccessor = transactionAccessor;
    }

    public async Task<long> EnqueueAsync<TPayload>(string jobType, 
        TPayload payload, 
        EnqueueOptions? options, 
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobType);

        options ??= new EnqueueOptions();

        if (options.MaxAttempts <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(options.MaxAttempts));

        var json = JsonSerializer.Serialize(payload, JsonOptions);

        var availableAt = DateTimeOffset.UtcNow + (options.Delay ?? TimeSpan.Zero);

        var connection = _transactionAccessor?.CurrentConnection;

        var transaction = _transactionAccessor?.CurrentTransaction;

        if (connection is not null &&
            transaction is not null)
        {
            return await InsertAsync(
                connection,
                transaction,
                jobType,
                json,
                options,
                availableAt,
                cancellationToken);
        }

        await using var ownConnection = await _dataSource.OpenConnectionAsync(cancellationToken);

        return await InsertAsync(
            ownConnection,
            null,
            jobType,
            json,
            options,
            availableAt,
            cancellationToken);
    }

    private static async Task<long> InsertAsync(
        DbConnection connection,
        DbTransaction? transaction,
        string jobType,
        string json,
        EnqueueOptions options,
        DateTimeOffset availableAt,
        CancellationToken cancellationToken)
    {
        await using var command =
            connection.CreateCommand();

        command.CommandText =
            PgQueueSql.InsertJob;

        if (transaction is not null)
            command.Transaction = transaction;

        AddParameter(
            command,
            "jobKey",
            options.JobKey);

        AddParameter(
            command,
            "jobType",
            jobType);

        AddParameter(
            command,
            "payload",
            json);

        AddParameter(
            command,
            "maxAttempts",
            options.MaxAttempts);

        AddParameter(
            command,
            "availableAt",
            availableAt);

        var result = await command.ExecuteScalarAsync(cancellationToken);

        if (result is null || result == DBNull.Value)
        {
            if (options.JobKey is not null)
                throw new DuplicateJobKeyException(options.JobKey);

            throw new InvalidOperationException(
                "Job was not inserted.");
        }

        return Convert.ToInt64(result);
    }

    private static void AddParameter(
        DbCommand command,
        string name,
        object? value)
    {
        var parameter =
            command.CreateParameter();

        parameter.ParameterName = name;
        parameter.Value =
            value ?? DBNull.Value;

        command.Parameters.Add(parameter);
    }
}
