using Dapper;
using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Data.Repositories;
using IisLogParserArcGIS.Domain.Aggregation;
using IisLogParserArcGIS.Reports.Sections;
using Microsoft.Data.Sqlite;

namespace IisLogParserArcGIS.Reports.Tests.TestSupport;

/// <summary>
/// A private, mutable copy of a harvested aggregate database file (see
/// <see cref="HarvestedAggregateDatabaseFixture"/>), for a test that needs edge-case rows the real corpus
/// doesn't naturally contain - an exact ranking tie between two entities, or an entity that only appears for
/// part of the date range - inserted directly rather than hand-building an entire fixture database from
/// scratch. Copying first keeps every other test in the fixture's collection reading the untouched, shared
/// harvested database.
/// </summary>
public sealed class WorkingDatabaseCopy : IDisposable
{
    private static readonly ByArcGisServiceRepository _arcGisServiceRepository = new();
    private static readonly ByPortalItemRepository _portalItemRepository = new();

    /// <summary>
    /// Copies <paramref name="sourceDatabasePath"/> to a new, private temporary path.
    /// </summary>
    /// <param name="sourceDatabasePath">The harvested database file to copy, e.g. a fixture's <c>DatabasePath</c>.</param>
    public WorkingDatabaseCopy(string sourceDatabasePath)
    {
        ArgumentNullException.ThrowIfNull(sourceDatabasePath);

        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid():N}.sqlite");
        File.Copy(sourceDatabasePath, Path);
    }

    /// <summary>
    /// Gets the full path of this working copy.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Inserts extra <see cref="ByArcGisServiceAggregateRow"/> rows into this working copy.
    /// </summary>
    /// <param name="rows">The edge-case rows to insert.</param>
    public void InsertArcGisServiceRows(params ByArcGisServiceAggregateRow[] rows)
    {
        using var connection = SqliteConnectionFactory.Open(Path);
        _arcGisServiceRepository.Insert(connection, rows);
    }

    /// <summary>
    /// Inserts extra <see cref="ByPortalItemAggregateRow"/> rows into this working copy.
    /// </summary>
    /// <param name="rows">The edge-case rows to insert.</param>
    public void InsertPortalItemRows(params ByPortalItemAggregateRow[] rows)
    {
        using var connection = SqliteConnectionFactory.Open(Path);
        _portalItemRepository.Insert(connection, rows);
    }

    /// <summary>
    /// Inserts extra <see cref="DeviceTestRow"/> rows into <paramref name="section"/>'s by-device table in this
    /// working copy. The Field Maps and Survey123 tables have identical columns, so one insert serves both.
    /// </summary>
    /// <param name="section">The per-device section whose table receives the rows.</param>
    /// <param name="rows">The edge-case rows to insert.</param>
    public void InsertDeviceRows(DeviceSection section, params DeviceTestRow[] rows)
    {
        ArgumentNullException.ThrowIfNull(section);

        using var connection = SqliteConnectionFactory.Open(Path);
        connection.Execute(
            $"""
            INSERT INTO {section.Table.Name} (local_date, device_id, username, time_taken_second, hits)
            VALUES (@LocalDate, @DeviceId, @Username, @TimeTakenSecond, @Hits)
            """,
            rows.Select(row => new { row.LocalDate, row.DeviceId, row.Username, TimeTakenSecond = (double)row.Hits, row.Hits }));
    }

    /// <summary>
    /// Deletes every row of <paramref name="section"/>'s by-device table in this working copy, leaving the table
    /// in place but empty.
    /// </summary>
    /// <param name="section">The per-device section whose table to empty.</param>
    public void DeleteAllDeviceRows(DeviceSection section)
    {
        ArgumentNullException.ThrowIfNull(section);

        using var connection = SqliteConnectionFactory.Open(Path);
        connection.Execute($"DELETE FROM {section.Table.Name}");
    }

    /// <summary>
    /// Drops <paramref name="section"/>'s by-device table from this working copy, to simulate a database created
    /// before that table was introduced.
    /// </summary>
    /// <param name="section">The per-device section whose table to drop.</param>
    public void DropDeviceTable(DeviceSection section)
    {
        ArgumentNullException.ThrowIfNull(section);

        using var connection = SqliteConnectionFactory.Open(Path);
        connection.Execute($"DROP TABLE {section.Table.Name}");
    }

    /// <summary>
    /// Opens a connection to this working copy, for a test's own assertions or further repository calls.
    /// </summary>
    public SqliteConnection OpenConnection() => SqliteConnectionFactory.Open(Path);

    /// <inheritdoc/>
    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        if (File.Exists(Path))
        {
            File.Delete(Path);
        }
    }
}
