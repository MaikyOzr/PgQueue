# PgQueue

**Durable job queue and transactional outbox for ASP.NET Core, built directly on PostgreSQL — no broker, no Redis, no new infrastructure.**

## The problem

You have an ASP.NET Core app backed by PostgreSQL. You need to reliably do something *after* a business transaction commits — send a confirmation email, deliver a webhook, trigger a downstream side effect. You reach for one of three options, and all of them have a catch:

1. **Hangfire** — mature, but built around fire-and-forget jobs with a dashboard. Enqueuing atomically inside the same transaction as your domain data means bolting your own outbox on top of it anyway.
2. **MassTransit + RabbitMQ / Azure Service Bus** — the correct way to do this at scale, but it means standing up and operating a message broker for what might be two or three job types.
3. **A hand-rolled `Jobs` table + a `BackgroundService` that polls it** — what most teams actually end up writing. And almost always with the same five bugs:

```csharp
var job = await db.Jobs
    .Where(j => j.Status == "pending")
    .OrderBy(j => j.CreatedAt)
    .FirstOrDefaultAsync(); // 🔴 two workers can grab the same row
```

| Bug | What actually happens in production |
|---|---|
| No `SKIP LOCKED` | Two pods race for the same row → the same job (e.g. an email) runs twice |
| `Task.Delay(N)` polling loop | Either wasted `SELECT`s every few seconds, or up-to-N-seconds latency on real work |
| Retry without backoff | A failing downstream call gets hammered instantly and repeatedly — a self-inflicted retry storm |
| No idempotency key | A crash between "job executed" and "status = done" replays the side effect on restart |
| Enqueue outside the business transaction | Business data commits, the job insert doesn't (or vice versa) — silent data loss |

Each of these is individually trivial. Getting all five right at once — `SKIP LOCKED`, `LISTEN/NOTIFY`, exponential backoff with jitter, dead-lettering, and true transactional enqueue — is not 40 lines, it's weeks of careful work that most teams don't have time for and re-derive badly, repeatedly, across the industry.

**PgQueue is that "done once, correctly" layer**, on top of the PostgreSQL instance your app is already running.

## What it gives you

- **Atomic enqueue** — insert a job in the *same* EF Core transaction as your business data. Rollback rolls back both.
- **`SKIP LOCKED` dequeue** — a single atomic `UPDATE ... WHERE id = (SELECT ... FOR UPDATE SKIP LOCKED)`. Concurrent workers physically cannot claim the same row.
- **`LISTEN/NOTIFY` wake-up** — near-instant pickup on an empty queue, with a polling fallback as a safety net (NOTIFY delivery isn't guaranteed across dropped connections).
- **Exponential backoff with jitter and dead-lettering** — configurable `MaxAttempts`, no retry storms.
- **Idempotency keys** — enqueue with a `JobKey`; a duplicate insert is a no-op, not a duplicate side effect.
- **Crash recovery** — jobs stuck in `processing` past a timeout are automatically reset and retried.

## What it deliberately does *not* do (yet)

No dashboard, no cron/recurring jobs, no priorities, no multi-queue routing, no fan-out/routing patterns. If you need those — or you're already running RabbitMQ/Service Bus for other reasons — MassTransit or Hangfire are the better fit. PgQueue exists for the narrower case: **you don't want new infrastructure for a handful of reliable background jobs, and your data already lives in Postgres.**

## Quick example

```csharp
builder.Services.AddPgQueue(options =>
{
    options.ConnectionString = builder.Configuration.GetConnectionString("Default");
    options.WorkerCount = 4;
});

builder.Services.AddPgQueueEntityFrameworkCore<AppDbContext>();
builder.Services.AddJobHandler<SendConfirmationEmailHandler, SendConfirmationEmailPayload>("send-confirmation-email");
```

```csharp
public async Task CreateOrderAsync(Order order, AppDbContext db, IPgQueue queue)
{
    await using var tx = await db.Database.BeginTransactionAsync();

    db.Orders.Add(order);
    await db.SaveChangesAsync();

    await queue.EnqueueAsync(
        "send-confirmation-email",
        new SendConfirmationEmailPayload(order.Id),
        new EnqueueOptions { JobKey = $"confirm-email:{order.Id}" });

    await tx.CommitAsync(); // order + job commit atomically, or neither does
}
```

```csharp
public class SendConfirmationEmailHandler : IJobHandler<SendConfirmationEmailPayload>
{
    public async Task HandleAsync(SendConfirmationEmailPayload payload, JobContext context, CancellationToken ct)
        => await _emailSender.SendAsync(payload.OrderId, ct);
}
```

## Project structure

```
PgQueue.Abstractions/       — public contracts (IPgQueue, IJobHandler<T>, EnqueueOptions) — zero infra dependencies
PgQueue.Core/                — SKIP LOCKED / LISTEN-NOTIFY worker, backoff, dispatch, DI registration
PgQueue.EntityFrameworkCore/ — transactional enqueue via DbContext.Database.CurrentTransaction
PgQueue.Testing/             — unit + DI-pipeline tests
OrderService.Api/            — sample ASP.NET Core app
```

## Status

Early / MVP. The core mechanics (transactional enqueue, `SKIP LOCKED`, backoff, dead-lettering, crash recovery) are implemented and tested. Not yet published to NuGet — clone and reference the projects directly for now.
