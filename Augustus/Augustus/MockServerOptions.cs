namespace Augustus;

/// <summary>
/// Configuration options for the <see cref="MockServer"/>.
/// </summary>
public class MockServerOptions
{
    private int _port = 9001;

    /// <summary>
    /// Gets or sets the TCP port number on which the mock server will listen.
    /// </summary>
    /// <value>
    /// The port number. Must be between 1024 and 65535. Default is 9001.
    /// </value>
    /// <remarks>
    /// Ports below 1024 are typically reserved for system services and require elevated privileges.
    /// If the specified port is already in use, starting the server will fail.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the value is less than 1024 or greater than 65535.</exception>
    public int Port
    {
        get => _port;
        set
        {
            if (value < 1024 || value > 65535)
                throw new ArgumentOutOfRangeException(nameof(Port), "Port must be between 1024 and 65535");
            _port = value;
        }
    }
}
