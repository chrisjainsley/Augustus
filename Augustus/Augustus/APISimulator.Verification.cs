namespace Augustus;

/// <summary>
/// Request recording and verification extensions for the API simulator.
/// </summary>
public sealed partial class APISimulator
{
    private readonly RequestRecorder requestRecorder = new();

    /// <summary>
    /// Records a request internally. Called by the routing handler on every incoming request.
    /// </summary>
    internal void RecordRequest(RecordedRequest request) => requestRecorder.Record(request);

    /// <summary>
    /// Gets all requests that have been received by the simulator, in the order they were recorded.
    /// </summary>
    /// <remarks>
    /// Each access returns a snapshot of the recorded requests at that point in time.
    /// Recording is automatic and always on — every request to the simulator is captured.
    /// </remarks>
    public IReadOnlyList<RecordedRequest> ReceivedRequests => requestRecorder.GetRecordedRequests();

    /// <summary>
    /// Creates a verification builder for asserting that requests matching the given predicate
    /// were called the expected number of times.
    /// </summary>
    /// <param name="predicate">A filter applied to recorded requests (e.g., <c>r => r.Path == "/v1/customers"</c>).</param>
    /// <returns>A <see cref="VerificationBuilder"/> for fluent call-count assertions.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate"/> is null.</exception>
    /// <example>
    /// <code>
    /// simulator.Verify(r => r.Method == HttpMethod.Post &amp;&amp; r.Path == "/v1/customers")
    ///     .WasCalledOnce();
    ///
    /// simulator.Verify(r => r.Path == "/v1/refunds")
    ///     .WasNeverCalled();
    /// </code>
    /// </example>
    public VerificationBuilder Verify(Func<RecordedRequest, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        var snapshot = requestRecorder.GetRecordedRequests();
        return new VerificationBuilder(snapshot, predicate);
    }

    /// <summary>
    /// Clears all recorded requests. Useful for test isolation when reusing a simulator across test phases.
    /// </summary>
    public void ClearRecordedRequests()
    {
        requestRecorder.Clear();
    }
}
