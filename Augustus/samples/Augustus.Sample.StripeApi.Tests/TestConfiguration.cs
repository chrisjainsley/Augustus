using System.Reflection;
using System.Runtime.CompilerServices;
using Augustus;
using Augustus.AI;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace Augustus.Sample.StripeApi.Tests;

internal static class TestConfiguration
{
    /// <summary>
    /// When set to <c>1</c> or <c>true</c>, Stripe sample tests run in cache-only mode against committed <c>__mocks__</c>
    /// (no OpenAI API key). GitHub Actions sets this for the <c>stripe-sample</c> job.
    /// </summary>
    public const string CiCacheOnlyEnvironmentVariable = "AUGUSTUS_STRIPE_SAMPLE_CI_CACHE_ONLY";

    private static readonly IConfigurationRoot Config = new ConfigurationBuilder()
        .AddUserSecrets(Assembly.GetExecutingAssembly())
        .AddEnvironmentVariables()
        .Build();

    /// <summary>
    /// Stripe sample CI mode: explicit env switch (not inferred from a missing API key).
    /// </summary>
    public static bool IsStripeSampleCiCacheOnly()
    {
        var v = Environment.GetEnvironmentVariable(CiCacheOnlyEnvironmentVariable);
        return string.Equals(v, "1", StringComparison.Ordinal)
            || string.Equals(v, "true", StringComparison.OrdinalIgnoreCase);
    }

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

    /// <summary>
    /// Applies CI cache-only or local AI-backed options to the Stripe simulator.
    /// </summary>
    public static void ApplyStripeSimulatorTransport(APISimulatorOptions options, int localPort)
    {
        if (IsStripeSampleCiCacheOnly())
        {
            options.Port = 0;
            options.CacheOnly = true;
        }
        else
        {
            options.Port = localPort;
        }
    }

    /// <summary>
    /// Installs the AI default handler; in CI cache-only mode no API key is required.
    /// </summary>
    public static void UseStripeAi(APISimulator simulator)
    {
        if (IsStripeSampleCiCacheOnly())
        {
            simulator.UseAI(new AIOptions());
            return;
        }

        var key = GetApiKey() ?? throw new InvalidOperationException("OpenAI API key required for local Stripe sample tests (user secrets or OPENAI_API_KEY).");
        simulator.UseAI(new AIOptions
        {
            OpenAIApiKey = key,
            OpenAIModel = GetModel()
        });
    }

    public static void AssertCommittedMockExists(string requestHash, string apiName = "Stripe", [CallerFilePath] string callerFilePath = "")
    {
        var path = Path.Combine(ResolveCachePath(apiName, callerFilePath), requestHash + ".json");
        File.Exists(path).Should().BeTrue("committed mock must exist at {0}", path);
    }
}
