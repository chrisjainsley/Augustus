using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;

namespace Augustus.Sample.StripeApi.Tests;

internal static class TestConfiguration
{
    private static readonly IConfigurationRoot Config = new ConfigurationBuilder()
        .AddUserSecrets(Assembly.GetExecutingAssembly())
        .AddEnvironmentVariables()
        .Build();

    public static string? GetApiKey()
    {
        return Config["OpenAI:ApiKey"]
            ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    }

    public static string GetModel()
    {
        return Config["OpenAI:Model"] ?? "gpt-4o-mini";
    }

    public static string ResolveCachePath(string apiName, [CallerFilePath] string callerFilePath = "")
    {
        var dir = Path.GetDirectoryName(callerFilePath)!;
        var className = Path.GetFileNameWithoutExtension(callerFilePath);
        return Path.Combine(dir, "__mocks__", className, apiName);
    }
}
