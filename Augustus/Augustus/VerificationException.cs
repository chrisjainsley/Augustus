namespace Augustus;

/// <summary>
/// Exception thrown when a request verification assertion fails.
/// </summary>
public sealed class VerificationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VerificationException"/> class with a descriptive message.
    /// </summary>
    /// <param name="message">A message describing the verification failure, including expected and actual call counts.</param>
    public VerificationException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VerificationException"/> class with a message and inner exception.
    /// </summary>
    public VerificationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
