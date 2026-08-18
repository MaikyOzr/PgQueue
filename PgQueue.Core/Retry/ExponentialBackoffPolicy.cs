namespace PgQueue.Core.Retry;

public static class ExponentialBackoffPolicy
{
    public static TimeSpan Calculate(int attempt)
    {
        var exponential = Math.Pow(2, attempt);

        var jitter = Random.Shared.Next(0, 5);

        var seconds = Math.Min(
            exponential + jitter,
            300);

        return TimeSpan.FromSeconds(seconds);
    }
}
