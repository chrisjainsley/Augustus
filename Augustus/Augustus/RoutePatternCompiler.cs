using System.Text.RegularExpressions;

namespace Augustus;

/// <summary>
/// Shared helper for compiling route patterns with named placeholders into regex.
/// Supports arbitrary <c>{name}</c> placeholders for single path segments and <c>{*}</c> for wildcards.
/// </summary>
internal static class RoutePatternCompiler
{
    /// <summary>
    /// Compiles a route pattern into a regex that matches request paths.
    /// <c>{name}</c> matches a single path segment (<c>[^/]+</c>),
    /// <c>{*}</c> matches any number of segments (<c>.*</c>).
    /// </summary>
    public static Regex Compile(string pattern)
    {
        try
        {
            int counter = 0;
            var paramMarkers = new List<string>();
            var preprocessed = Regex.Replace(pattern, @"\{([^}]+)\}", match =>
            {
                if (match.Groups[1].Value == "*")
                    return "\x00WILD\x00";
                var marker = $"\x00P{counter++}\x00";
                paramMarkers.Add(marker);
                return marker;
            });

            var regexPattern = Regex.Escape(preprocessed)
                .Replace("\x00WILD\x00", ".*");

            foreach (var marker in paramMarkers)
                regexPattern = regexPattern.Replace(marker, @"[^/]+");

            return new Regex($"^{regexPattern}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }
        catch (ArgumentException)
        {
            return new Regex($"^{Regex.Escape(pattern)}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }
    }
}
