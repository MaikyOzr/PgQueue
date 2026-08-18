using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PgQueue.Core.Internal;
using System.Data.Common;

namespace PgQueue.EntityFrameworkCore;

public sealed class EfCoreTransactionAccessor<TDbContext> : IPgQueueTransactionAccessor where TDbContext : DbContext
{
    private readonly TDbContext _dbContext;

    public EfCoreTransactionAccessor(TDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public DbConnection? CurrentConnection => _dbContext.Database.CurrentTransaction?.GetDbTransaction()?.Connection;

    public DbTransaction? CurrentTransaction => _dbContext.Database.CurrentTransaction?.GetDbTransaction();
}