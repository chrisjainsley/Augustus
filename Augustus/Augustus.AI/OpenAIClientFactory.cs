using System.ClientModel;
using System.ClientModel.Primitives;
using Azure.AI.OpenAI;
using OpenAI;

namespace Augustus.AI;

/// <summary>
/// Builds the chat client for a model tier: Azure OpenAI, OpenAI, or any OpenAI-compatible endpoint.
/// </summary>
internal static class OpenAIClientFactory
{
    public static OpenAIClient Create(AIOptions options, AIModelTier tier)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(tier);

        var credential = new ApiKeyCredential(options.ResolveApiKey(tier));
        var transport = options.HttpHandlerOverride is null
            ? null
            : new HttpClientPipelineTransport(new HttpClient(options.HttpHandlerOverride, disposeHandler: false));

        if (options.UseAzureOpenAI && tier.Endpoint is null)
        {
            var azureOptions = new AzureOpenAIClientOptions();
            if (transport is not null)
                azureOptions.Transport = transport;
            return new AzureOpenAIClient(new Uri(options.OpenAIEndpoint), credential, azureOptions);
        }

        var clientOptions = new OpenAIClientOptions();
        var endpoint = tier.Endpoint ?? (string.IsNullOrEmpty(options.OpenAIEndpoint) ? null : options.OpenAIEndpoint);
        if (endpoint is not null)
            clientOptions.Endpoint = new Uri(endpoint);
        if (transport is not null)
            clientOptions.Transport = transport;
        return new OpenAIClient(credential, clientOptions);
    }
}
