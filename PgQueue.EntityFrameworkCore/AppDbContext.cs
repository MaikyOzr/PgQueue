using Microsoft.EntityFrameworkCore;

namespace PgQueue.EntityFrameworkCore;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(
        DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
}
