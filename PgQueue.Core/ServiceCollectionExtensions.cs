using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using PgQueue.Abstractions;
using PgQueue.Core.Dispatch;
using PgQueue.Core.Worker;
using PgQueue.Core.Schema;

namespace PgQueue.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPgQueue(
        this IServiceCollection services,
        Action<PgQueueOptions> configure)
    {
        var options = new PgQueueOptions
        {
            ConnectionString = string.Empty
        };

        configure(options);

        var dataSource = NpgsqlDataSource.Create(options.ConnectionString);

        services.AddSingleton(dataSource);

        // Ensure schema is created on first use
        EnsureSchema(dataSource);

        services.AddSingleton(
            new PgQueueWorkerOptions
            {
                WorkerCount = options.WorkerCount,
                PollingFallbackInterval = options.PollingFallbackInterval
            });

        services.AddScoped<IPgQueue, PgQueueService>();

        services.AddSingleton<IJobHandlerRegistry, JobHandlerRegistry>();

        services.AddSingleton<JobDispatcher>();

        services.AddHostedService<PgQueueBackgroundService>();

        return services;
    }

    public static IServiceCollection AddJobHandler<THandler,TPayload>(
        this IServiceCollection services,
        string jobType)
        where THandler : class, IJobHandler<TPayload>
    {
        services.AddScoped<THandler>();

        services.AddSingleton<IConfigureJobHandlerRegistry>(
            new ConfigureJobHandlerRegistry<THandler, TPayload>(jobType));

        return services;
    }

    private static void EnsureSchema(NpgsqlDataSource dataSource)
    {
        using var connection = dataSource.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = PgQueueSchemaSql.EnsureCreated;
        command.ExecuteNonQuery();
    }
}