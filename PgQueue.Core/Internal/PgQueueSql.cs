namespace PgQueue.Core.Internal;

internal static class PgQueueSql
{
    public const string InsertJob = """
        INSERT INTO pgqueue_jobs
        (
            job_key,
            job_type,
            payload,
            max_attempts,
            available_at
        )
        VALUES
        (
            @jobKey,
            @jobType,
            @payload::jsonb,
            @maxAttempts,
            @availableAt
        )
        ON CONFLICT (job_key) DO NOTHING
        RETURNING id;
        """;

    public const string Dequeue = """
        UPDATE pgqueue_jobs
        SET
            status = 1,
            locked_by = @workerId,
            locked_at = now()
        WHERE id =
        (
            SELECT id
            FROM pgqueue_jobs
            WHERE status = 0
              AND available_at <= now()
            ORDER BY available_at, id
            FOR UPDATE SKIP LOCKED
            LIMIT 1
        )
        RETURNING
            id,
            job_type,
            payload,
            attempts,
            max_attempts,
            created_at;
        """;

    public const string CompleteJob = """
        UPDATE pgqueue_jobs
        SET
            status = 2,
            locked_by = NULL,
            locked_at = NULL
        WHERE id = @id;
        """;

    public const string FailJobRetry = """
        UPDATE pgqueue_jobs
        SET
            status = 0,
            attempts = attempts + 1,
            available_at = @availableAt,
            last_error = @error,
            locked_by = NULL,
            locked_at = NULL
        WHERE id = @id;
        """;

    public const string FailJobDead = """
        UPDATE pgqueue_jobs
        SET
            status = 3,
            attempts = attempts + 1,
            last_error = @error,
            locked_by = NULL,
            locked_at = NULL
        WHERE id = @id;
        """;
}