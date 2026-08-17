using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PgQueue.Abstractions;
using PgQueue.Core;
using PgQueue.Core.Internal;

namespace PgQueue.EntityFrameworkCore;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPgQueueEntityFrameworkCore<TDbContext>(this IServiceCollection services)
    where TDbContext : DbContext
    {
        services.AddScoped<IPgQueue, PgQueueService>();

        return services;
    }
}
