namespace Augustus;

/// <summary>
/// Configuration for Gaussian latency simulation applied to mock responses.
/// </summary>
internal sealed class LatencyOptions
{
    /// <summary>
    /// Gets the mean delay in milliseconds.
    /// </summary>
    public int MeanMs { get; }

    /// <summary>
    /// Gets the standard deviation of the delay in milliseconds.
    /// </summary>
    public int StdDevMs { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LatencyOptions"/> class.
    /// </summary>
    /// <param name="meanMs">The mean delay in milliseconds. Must be non-negative.</param>
    /// <param name="stdDevMs">The standard deviation of the delay in milliseconds. Must be non-negative.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="meanMs"/> or <paramref name="stdDevMs"/> is negative.
    /// </exception>
    public LatencyOptions(int meanMs, int stdDevMs)
    {
        if (meanMs < 0) throw new ArgumentOutOfRangeException(nameof(meanMs), "Mean latency must be non-negative.");
        if (stdDevMs < 0) throw new ArgumentOutOfRangeException(nameof(stdDevMs), "Standard deviation must be non-negative.");

        MeanMs = meanMs;
        StdDevMs = stdDevMs;
    }
}
