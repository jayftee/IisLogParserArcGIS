using System.Text.Json;
using IisLogParserArcGIS.Cli;
using Microsoft.Data.Sqlite;

namespace IisLogParserArcGIS.Tests.TestSupport;

/// <summary>
/// A throwaway directory tree for one CLI runner test: a <c>base</c> directory standing in for the executable's
/// own directory (holds <c>appsettings.json</c>, its overlays, the log and Dashboard output), plus separate
/// <c>logs-in</c> and <c>data</c> directories for the harvest's log source and the aggregate database. Disposal
/// releases only the pooled connections of the databases this workspace handed out (a global
/// <c>SqliteConnection.ClearAllPools()</c> would close connections other parallel tests still use), then deletes
/// the whole tree.
/// </summary>
internal sealed class CliTestWorkspace : IDisposable
{
    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("iislogparser-cli-tests-");
    private readonly List<string> _databasePaths = [];

    public CliTestWorkspace()
    {
        BaseDirectory = Directory.CreateDirectory(Path.Combine(_root.FullName, "base")).FullName + Path.DirectorySeparatorChar;
        LogSourceDirectory = Directory.CreateDirectory(Path.Combine(_root.FullName, "logs-in")).FullName;
        DataDirectory = Directory.CreateDirectory(Path.Combine(_root.FullName, "data")).FullName;
    }

    /// <summary>Gets the stand-in executable directory; ends in a separator, like <c>AppContext.BaseDirectory</c>.</summary>
    public string BaseDirectory { get; }

    public string LogSourceDirectory { get; }

    public string DataDirectory { get; }

    /// <summary>Gets everything the runners wrote to their error writer so far.</summary>
    public StringWriter Error { get; } = new();

    public RunEnvironment Environment(string environmentName = "Production")
    {
        return new RunEnvironment(BaseDirectory, environmentName, Error, TimeProvider.System);
    }

    public string CreateDirectoryUnderRoot(string name)
    {
        return Directory.CreateDirectory(Path.Combine(_root.FullName, name)).FullName;
    }

    public string DatabasePath(string fileName = "aggregates.sqlite")
    {
        var path = Path.Combine(DataDirectory, fileName);
        _databasePaths.Add(path);
        return path;
    }

    /// <summary>
    /// Writes <c>appsettings.json</c> into the base directory: a complete, valid default set of values, with
    /// <paramref name="overrides"/> replacing individual keys.
    /// </summary>
    public void WriteAppSettings(IReadOnlyDictionary<string, object?>? overrides = null)
    {
        var values = new Dictionary<string, object?>
        {
            ["LocalTimeZone"] = "UTC",
            ["LogOutputDirectory"] = "Logs",
            ["LogLevel"] = "Warning",
            ["PortalWebAdaptorName"] = "portal",
            ["IncludedRoots"] = Array.Empty<string>(),
            ["PortalBaseUrl"] = "portal.example.com",
            ["ArcGisServerBaseUrl"] = "gis.example.com",
            ["OutputDirectory"] = "Dashboard",
        };

        foreach (var (key, value) in overrides ?? new Dictionary<string, object?>())
        {
            values[key] = value;
        }

        File.WriteAllText(Path.Combine(BaseDirectory, "appsettings.json"), JsonSerializer.Serialize(values));
    }

    public void WriteOverlay(string environmentName, IReadOnlyDictionary<string, object?> values)
    {
        File.WriteAllText(Path.Combine(BaseDirectory, $"appsettings.{environmentName}.json"), JsonSerializer.Serialize(values));
    }

    public string WriteLogFile(string fileName, params string[] lines)
    {
        var path = Path.Combine(LogSourceDirectory, fileName);
        File.WriteAllLines(path, lines);
        return path;
    }

    public void Dispose()
    {
        foreach (var databasePath in _databasePaths)
        {
            using var pooledConnection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = databasePath }.ConnectionString);
            SqliteConnection.ClearPool(pooledConnection);
        }

        Error.Dispose();
        _root.Delete(recursive: true);
    }
}
