using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Augustus.AI.Tests;

/// <summary>
/// In-process HTTP transport for model and TypeSafe calls: records each request and answers from a callback.
/// </summary>
internal sealed class StubHttpHandler : HttpMessageHandler
{
    private readonly Func<RecordedRequest, HttpResponseMessage> respond;
    private readonly List<RecordedRequest> requests = new();

    public StubHttpHandler(Func<RecordedRequest, HttpResponseMessage> respond)
    {
        this.respond = respond;
    }

    public IReadOnlyList<RecordedRequest> Requests
    {
        get
        {
            lock (requests)
                return requests.ToList();
        }
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
        var recorded = new RecordedRequest(request.RequestUri!, body);
        lock (requests)
            requests.Add(recorded);
        return respond(recorded);
    }

    /// <summary>A chat completion whose single choice carries <paramref name="content"/>.</summary>
    public static HttpResponseMessage ChatCompletion(string content)
    {
        var json = new JsonObject
        {
            ["id"] = "chatcmpl-test",
            ["object"] = "chat.completion",
            ["created"] = 1,
            ["model"] = "stub",
            ["choices"] = new JsonArray(new JsonObject
            {
                ["index"] = 0,
                ["message"] = new JsonObject { ["role"] = "assistant", ["content"] = content },
                ["finish_reason"] = "stop"
            }),
            ["usage"] = new JsonObject { ["prompt_tokens"] = 1, ["completion_tokens"] = 1, ["total_tokens"] = 2 }
        };
        return Json(json.ToJsonString());
    }

    public static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK)
        => new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
}

internal sealed record RecordedRequest(Uri Uri, string Body)
{
    public JsonElement Json => JsonDocument.Parse(Body).RootElement;

    public string? Model => Json.TryGetProperty("model", out var model) ? model.GetString() : null;
}
