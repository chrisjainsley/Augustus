using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace Augustus.Sample.Reqnroll.Specs.Support;

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
}
