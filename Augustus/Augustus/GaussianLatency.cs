namespace Augustus;

/// <summary>
/// Generates latency values from a Gaussian (normal) distribution using the Box-Muller transform.
/// </summary>
internal static class GaussianLatency
{
    /// <summary>
    /// Samples a non-negative delay in milliseconds from a Gaussian distribution.
    /// </summary>
    /// <param name="meanMs">The mean delay in milliseconds.</param>
    /// <param name="stdDevMs">The standard deviation in milliseconds.</param>
    /// <param name="random">Optional random instance for testability. Uses <see cref="Random.Shared"/> if null.</param>
    /// <returns>A non-negative delay in milliseconds, drawn from N(meanMs, stdDevMs) and clamped to >= 0.</returns>
    internal static int Sample(int meanMs, int stdDevMs, Random? random = null)
    {
        if (stdDevMs == 0)
            return Math.Max(0, meanMs);

        var rng = random ?? Random.Shared;

        // Box-Muller transform: convert two uniform random values to a standard normal value
        double u1, u2;
        do
        {
            u1 = rng.NextDouble();
        } while (u1 == 0); // u1 must be > 0 for Log

        u2 = rng.NextDouble();

        var gaussian = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        var delay = (int)(meanMs + stdDevMs * gaussian);

        return Math.Max(0, delay);
    }
}
