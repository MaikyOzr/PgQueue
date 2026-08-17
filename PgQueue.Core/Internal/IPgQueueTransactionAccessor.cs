using System.Data.Common;

namespace PgQueue.Core.Internal;

public interface IPgQueueTransactionAccessor
{
    DbConnection? CurrentConnection { get; }
    DbTransaction? CurrentTransaction { get; }
}
