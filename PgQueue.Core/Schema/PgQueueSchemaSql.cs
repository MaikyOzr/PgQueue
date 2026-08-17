namespace PgQueue.Core.Schema;

internal static class PgQueueSchemaSql
{
    public const string EnsureCreated = """
        CREATE TABLE IF NOT EXISTS pgqueue_jobs
        (
            id BIGSERIAL PRIMARY KEY,

            job_key TEXT UNIQUE,

            job_type TEXT NOT NULL,

            payload JSONB NOT NULL,

            status SMALLINT NOT NULL DEFAULT 0,

            attempts INT NOT NULL DEFAULT 0,

            max_attempts INT NOT NULL DEFAULT 5,

            available_at TIMESTAMPTZ NOT NULL DEFAULT now(),

            locked_by UUID,

            locked_at TIMESTAMPTZ,

            created_at TIMESTAMPTZ NOT NULL DEFAULT now(),

            last_error TEXT
        );

        CREATE INDEX IF NOT EXISTS idx_pgqueue_dequeue
            ON pgqueue_jobs (available_at)
            WHERE status = 0;

        CREATE OR REPLACE FUNCTION pgqueue_notify()
        RETURNS trigger
        AS $$
        BEGIN
            PERFORM pg_notify(
                'pgqueue_new_job',
                NEW.job_type
            );

            RETURN NEW;
        END;
        $$ LANGUAGE plpgsql;

        DROP TRIGGER IF EXISTS pgqueue_notify_trigger
            ON pgqueue_jobs;

        CREATE TRIGGER pgqueue_notify_trigger
            AFTER INSERT ON pgqueue_jobs
            FOR EACH ROW
            EXECUTE FUNCTION pgqueue_notify();
        """;
}
