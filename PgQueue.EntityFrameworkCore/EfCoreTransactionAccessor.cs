using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PgQueue.Core.Internal;
using System.Data.Common;

namespace PgQueue.EntityFrameworkCore;

internal sealed class EfCoreTransactionAccessor : IPgQueueTransactionAccessor
{
    private readonly DbContext _dbContext;

    public EfCoreTransactionAccessor(
        DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public DbConnection? CurrentConnection => _dbContext.Database.CurrentTransaction?.GetDbTransaction()?.Connection;

    public DbTransaction? CurrentTransaction => _dbContext.Database.CurrentTransaction?.GetDbTransaction();
}