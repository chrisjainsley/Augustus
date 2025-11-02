namespace Augustus.APIs.Stripe;

using Augustus;
using System.Text.Json;

/// <summary>
/// Configures response strategies for a Stripe API endpoint.
/// </summary>
public class StripeResourceConfigurer
{
    private readonly APISimulator apiSimulator;
    private readonly string pattern;
    private readonly string httpMethod;
    private readonly string resourceType;

    internal StripeResourceConfigurer(APISimulator apiSimulator, string pattern, string httpMethod, string resourceType)
    {
        this.apiSimulator = apiSimulator ?? throw new ArgumentNullException(nameof(apiSimulator));
        this.pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
        this.httpMethod = httpMethod ?? throw new ArgumentNullException(nameof(httpMethod));
        this.resourceType = resourceType ?? throw new ArgumentNullException(nameof(resourceType));
    }

    /// <summary>
    /// Uses the built-in default realistic Stripe response for this endpoint.
    /// </summary>
    public void UseDefault()
    {
        var defaultResponse = StripeDefaults.GetDefaultResponse(resourceType);
        apiSimulator.ForRoute(pattern, httpMethod)
            .WithResponse(defaultResponse)
            .Add();
    }

    /// <summary>
    /// Uses a custom JSON response string.
    /// </summary>
    /// <param name="jsonResponse">The JSON response to return.</param>
    public void WithResponse(string jsonResponse)
    {
        apiSimulator.ForRoute(pattern, httpMethod)
            .WithResponse(jsonResponse)
            .Add();
    }

    /// <summary>
    /// Uses a custom response object (will be serialized to JSON).
    /// </summary>
    /// <param name="responseObject">The object to serialize and return.</param>
    public void WithResponse(object responseObject)
    {
        apiSimulator.ForRoute(pattern, httpMethod)
            .WithResponse(responseObject)
            .Add();
    }

    /// <summary>
    /// Uses a JSON file for the response.
    /// </summary>
    /// <param name="filePath">Path to the JSON file.</param>
    public void WithJsonFile(string filePath)
    {
        apiSimulator.ForRoute(pattern, httpMethod)
            .WithJsonFile(filePath)
            .Add();
    }
}
