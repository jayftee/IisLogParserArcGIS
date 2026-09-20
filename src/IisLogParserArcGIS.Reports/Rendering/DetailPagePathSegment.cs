using System.Globalization;
using System.Text;

namespace IisLogParserArcGIS.Reports.Rendering;

/// <summary>
/// Sanitizes one Detail Page output-relative path segment (an ArcGIS Server folder/service name/service type, or
/// a Portal item id) before it is used to build a Dashboard file path. These values are parsed from untrusted,
/// attacker-influenced request URIs and, unlike a Root/Site, are never filtered against an operator-curated
/// allow-list - a scanner or malformed request can put any character a URI path segment allows (e.g. <c>*</c>,
/// <c>?</c>, <c>"</c>), or even a Windows-reserved device name (<c>con</c>, <c>nul</c>, …), into a folder or
/// service name, which would otherwise reach <see cref="StaticPageWriter"/> as-is and fail the whole Regeneration
/// Run when the filesystem rejects it as an illegal path.
/// </summary>
public static class DetailPagePathSegment
{
    private const char ReplacementChar = '_';
    private const string FallbackSegment = "_";

    // The Windows-illegal filename punctuation characters (Path.GetInvalidFileNameChars() on Windows also
    // includes every control character below 0x20, added below), used unconditionally regardless of the host OS
    // this code happens to run on: the Dashboard this segment names a file under is always meant to be served
    // from - and often built directly on - a Windows deployment, per this project's IIS/Task Scheduler context.
    private static readonly HashSet<char> _invalidChars = BuildInvalidChars();

    // Windows reserved device names: illegal as a file or directory name regardless of case or extension. Only
    // an exact (post-trim) match is checked here, not Windows' full "name before the first dot" rule (e.g.
    // "con.txt") - a deliberately accepted, narrower check, since folder/service-name/service-type/item-id values
    // are not expected to contain embedded dots in practice.
    private static readonly HashSet<string> _reservedDeviceNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "con", "prn", "aux", "nul",
        "com1", "com2", "com3", "com4", "com5", "com6", "com7", "com8", "com9",
        "lpt1", "lpt2", "lpt3", "lpt4", "lpt5", "lpt6", "lpt7", "lpt8", "lpt9",
    };

    /// <summary>
    /// Makes <paramref name="value"/> safe to use as one Dashboard output-relative path segment: replaces every
    /// character illegal in a Windows file/directory name with <c>_</c>, then strips trailing dots/spaces (which
    /// Windows itself silently strips, so leaving them in place risks two differently-spelled segments quietly
    /// landing on the same directory/file) and rejects a Windows-reserved device name outright. Whenever any of
    /// this changes the value, a short, deterministic suffix derived from the original value is appended, so two
    /// different raw values that would otherwise sanitize to the same result (e.g. <c>"item*1"</c> and
    /// <c>"item?1"</c>, both becoming <c>"item_1"</c>) never collide on disk. A value left untouched by every
    /// check above - the overwhelming common case for real ArcGIS Server/Portal identifiers - is returned exactly
    /// as given.
    /// </summary>
    /// <param name="value">The raw path segment to sanitize.</param>
    /// <returns>A value safe to use as one Dashboard output-relative path segment.</returns>
    public static string Sanitize(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Length == 0)
        {
            return FallbackSegment;
        }

        var sanitized = ReplaceInvalidChars(value).TrimEnd('.', ' ');

        if (sanitized.Length == 0 || _reservedDeviceNames.Contains(sanitized))
        {
            sanitized = FallbackSegment;
        }

        return string.Equals(sanitized, value, StringComparison.Ordinal)
            ? sanitized
            : $"{sanitized}-{StableSuffix(value)}";
    }

    private static string ReplaceInvalidChars(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            builder.Append(_invalidChars.Contains(c) ? ReplacementChar : c);
        }

        return builder.ToString();
    }

    // FNV-1a: a simple, non-cryptographic hash that is deterministic across runs and .NET versions - unlike
    // string.GetHashCode(), whose per-process randomization would make the same raw value sanitize differently
    // between runs. Only used to disambiguate colliding sanitized values; not a security boundary.
    private static string StableSuffix(string value)
    {
        var hash = 2166136261u;
        foreach (var b in Encoding.UTF8.GetBytes(value))
        {
            hash ^= b;
            hash *= 16777619u;
        }

        return hash.ToString("x8", CultureInfo.InvariantCulture);
    }

    private static HashSet<char> BuildInvalidChars()
    {
        char[] punctuationChars = ['"', '<', '>', '|', ':', '*', '?', '\\', '/'];
        var controlChars = Enumerable.Range(0, 32).Select(codePoint => (char)codePoint);

        return [.. punctuationChars, .. controlChars];
    }
}
