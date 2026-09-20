namespace IisLogParserArcGIS.Configuration;

/// <summary>
/// Resolves a configured directory path - relative or absolute - against a base directory, shared by every
/// setting (e.g. <see cref="AppSettings.LogOutputDirectory"/>, the Reports project's own output directory) that
/// accepts either form.
/// </summary>
public static class RelativePathResolver
{
    /// <summary>
    /// Resolves <paramref name="path"/> against <paramref name="baseDirectory"/> when it isn't already rooted.
    /// </summary>
    /// <param name="path">The configured path, relative or absolute.</param>
    /// <param name="baseDirectory">The directory a relative <paramref name="path"/> is resolved against.</param>
    /// <returns>The resolved path.</returns>
    public static string Resolve(string path, string baseDirectory)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(baseDirectory);

        return Path.IsPathRooted(path) ? path : Path.Combine(baseDirectory, path);
    }
}
