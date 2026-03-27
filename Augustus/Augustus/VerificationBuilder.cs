namespace Augustus;

using System.Text;

/// <summary>
/// Fluent builder for asserting that recorded requests match expected call counts.
/// </summary>
/// <remarks>
/// Returned by <see cref="APISimulator.Verify"/>. Operates on a snapshot of recorded requests
/// taken at the time <c>Verify()</c> was called, so it is safe to use after the simulator is stopped.
/// </remarks>
public sealed class VerificationBuilder
{
    private readonly IReadOnlyList<RecordedRequest> allRequests;
    private readonly Func<RecordedRequest, bool> predicate;

    internal VerificationBuilder(
        IReadOnlyList<RecordedRequest> allRequests,
        Func<RecordedRequest, bool> predicate)
    {
        this.allRequests = allRequests;
        this.predicate = predicate;
    }

    /// <summary>
    /// Asserts that exactly one matching request was recorded.
    /// </summary>
    /// <exception cref="VerificationException">Thrown when the matching request count is not exactly 1.</exception>
    public void WasCalledOnce()
    {
        WasCalledTimes(1);
    }

    /// <summary>
    /// Asserts that exactly <paramref name="expectedCount"/> matching requests were recorded.
    /// </summary>
    /// <param name="expectedCount">The exact number of expected matching requests.</param>
    /// <exception cref="VerificationException">Thrown when the actual count does not equal <paramref name="expectedCount"/>.</exception>
    public void WasCalledTimes(int expectedCount)
    {
        AssertCount(count => count == expectedCount, $"exactly {expectedCount}");
    }

    /// <summary>
    /// Asserts that no matching requests were recorded.
    /// </summary>
    /// <exception cref="VerificationException">Thrown when one or more matching requests were recorded.</exception>
    public void WasNeverCalled()
    {
        AssertCount(count => count == 0, "no");
    }

    /// <summary>
    /// Asserts that at least <paramref name="minimumCount"/> matching requests were recorded.
    /// </summary>
    /// <param name="minimumCount">The minimum number of expected matching requests.</param>
    /// <exception cref="VerificationException">Thrown when the actual count is less than <paramref name="minimumCount"/>.</exception>
    public void WasCalledAtLeast(int minimumCount)
    {
        AssertCount(count => count >= minimumCount, $"at least {minimumCount}");
    }

    /// <summary>
    /// Asserts that at most <paramref name="maximumCount"/> matching requests were recorded.
    /// </summary>
    /// <param name="maximumCount">The maximum number of expected matching requests.</param>
    /// <exception cref="VerificationException">Thrown when the actual count exceeds <paramref name="maximumCount"/>.</exception>
    public void WasCalledAtMost(int maximumCount)
    {
        AssertCount(count => count <= maximumCount, $"at most {maximumCount}");
    }

    private void AssertCount(Func<int, bool> passes, string expectationLabel)
    {
        var matchCount = allRequests.Count(predicate);
        if (!passes(matchCount))
        {
            throw new VerificationException(BuildMessage(expectationLabel, matchCount));
        }
    }

    private string BuildMessage(string expected, int actual)
    {
        const int maxDisplayed = 20;
        var sb = new StringBuilder();
        sb.AppendLine($"Expected {expected} matching request(s), but found {actual}.");
        sb.AppendLine();
        sb.AppendLine($"Recorded requests ({allRequests.Count} total):");

        var displayCount = Math.Min(allRequests.Count, maxDisplayed);
        for (var i = 0; i < displayCount; i++)
        {
            var r = allRequests[i];
            var marker = predicate(r) ? " [matched]" : "";
            sb.AppendLine($"  [{i}] {r.Method} {r.Path} ({r.Timestamp:O}){marker}");
        }

        if (allRequests.Count > maxDisplayed)
        {
            sb.AppendLine($"  ... and {allRequests.Count - maxDisplayed} more (inspect ReceivedRequests for full list)");
        }

        return sb.ToString().TrimEnd();
    }
}
