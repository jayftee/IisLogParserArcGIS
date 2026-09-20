namespace IisLogParserArcGIS.Reports.Rendering;

/// <summary>
/// Renders one <see cref="PageShellRequest"/> and writes it to its output-relative path under the Dashboard's
/// output directory, creating any intermediate directories as needed - the one place every section builder
/// turns a built page request into a file on disk.
/// </summary>
public static class StaticPageWriter
{
    /// <summary>
    /// Renders <paramref name="request"/> and writes it under <paramref name="outputDirectory"/>.
    /// </summary>
    /// <param name="outputDirectory">The Dashboard's output root directory.</param>
    /// <param name="request">The page to render and write.</param>
    public static void Write(string outputDirectory, PageShellRequest request)
    {
        ArgumentNullException.ThrowIfNull(outputDirectory);
        ArgumentNullException.ThrowIfNull(request);

        var html = PageShellRenderer.Render(request);
        var fullPath = Path.Combine(outputDirectory, request.OutputRelativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, html);
    }
}
