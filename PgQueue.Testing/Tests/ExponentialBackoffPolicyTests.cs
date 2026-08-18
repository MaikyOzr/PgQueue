using PgQueue.Core.Retry;
using Xunit;

namespace PgQueue.Testing.Tests;

public class ExponentialBackoffPolicyTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(8)]
    public void Calculate_WithinBaseAndJitterBounds_ForLowAttempts(int attempt)
    {
        var result = ExponentialBackoffPolicy.Calculate(attempt);

        var baseSeconds = Math.Pow(2, attempt);
        var expectedMin = TimeSpan.FromSeconds(baseSeconds);
        var expectedMax = TimeSpan.FromSeconds(Math.Min(baseSeconds + 4, 300));

        Assert.True(result >= expectedMin,
            $"attempt {attempt}: expected >= {expectedMin}, got {result}");
        Assert.True(result <= expectedMax,
            $"attempt {attempt}: expected <= {expectedMax}, got {result}");
    }

    [Theory]
    [InlineData(15)]
    [InlineData(20)]
    [InlineData(30)]
    public void Calculate_CapsAt300Seconds_ForHighAttempts(int attempt)
    {
        var result = ExponentialBackoffPolicy.Calculate(attempt);

        Assert.Equal(TimeSpan.FromSeconds(300), result);
    }

    [Fact]
    public void Calculate_NeverExceeds300Seconds_AcrossManySamples()
    {
        for (var attempt = 0; attempt <= 25; attempt++)
        {
            for (var sample = 0; sample < 20; sample++)
            {
                var result = ExponentialBackoffPolicy.Calculate(attempt);
                Assert.True(result <= TimeSpan.FromSeconds(300),
                    $"attempt {attempt} sample {sample} exceeded the cap: {result}");
            }
        }
    }

    [Fact]
    public void Calculate_JitterProducesVariation()
    {
        var results = Enumerable.Range(0, 50)
            .Select(_ => ExponentialBackoffPolicy.Calculate(2))
            .Distinct()
            .ToList();

        Assert.True(results.Count > 1,
            "jitter should produce more than one distinct value across 50 samples");
    }
}