using System.Data;
using IisLogParserArcGIS.Data.Repositories;
using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Domain.Aggregation;

namespace IisLogParserArcGIS.Data.Tests.TestSupport;

/// <summary>
/// Helpers for tests that run the same behaviour against both by-device tables (Field Maps and Survey123): the
/// xUnit theory data, and a single row builder and insert that go through whichever repository matches the table,
/// so no test keeps a per-table copy of either.
/// </summary>
public static class DeviceTableTestSupport
{
    /// <summary>
    /// Gets xUnit theory data with one case per by-device table, keyed by the table's name; resolve it back with
    /// <see cref="TableFor"/>.
    /// </summary>
    public static TheoryData<string> TableNames
    {
        get
        {
            var data = new TheoryData<string>();

            foreach (var table in AllTables)
            {
                data.Add(table.Name);
            }

            return data;
        }
    }

    private static DeviceAggregateTable[] AllTables => [DeviceAggregateTable.FieldMaps, DeviceAggregateTable.Survey123];

    public static DeviceAggregateTable TableFor(string tableName)
    {
        return AllTables.Single(table => table.Name == tableName);
    }

    /// <summary>
    /// Gets the by-device table that is not <paramref name="table"/>, for tests proving a query never reads the
    /// other table.
    /// </summary>
    public static DeviceAggregateTable OtherTable(DeviceAggregateTable table)
    {
        ArgumentNullException.ThrowIfNull(table);

        return AllTables.Single(other => other.Name != table.Name);
    }

#pragma warning disable CC0042 // Five independent row fields for a test-only fixture builder; bundling them would just repackage them.
    public static DeviceTestRow DeviceRow(string deviceId, string? username, DateOnly localDate, int hits, double? timeTakenSecond = null) => new()
    {
        LocalDate = localDate,
        DeviceId = deviceId,
        Username = username,
        TimeTakenSecond = timeTakenSecond ?? hits,
        Hits = hits,
    };
#pragma warning restore CC0042

    /// <summary>
    /// Inserts <paramref name="rows"/> into <paramref name="table"/> through the repository that owns it.
    /// </summary>
    /// <param name="connection">An open connection to a database that already has the aggregate schema.</param>
    /// <param name="table">The by-device table to insert into.</param>
    /// <param name="rows">The rows to insert.</param>
    public static void InsertRows(IDbConnection connection, DeviceAggregateTable table, params DeviceTestRow[] rows)
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(rows);

        if (table.Name == DeviceAggregateTable.FieldMaps.Name)
        {
            new ByFieldMapsDeviceRepository().Insert(connection, [.. rows.Select(ToFieldMapsRow)]);
        }
        else
        {
            new BySurvey123DeviceRepository().Insert(connection, [.. rows.Select(ToSurvey123Row)]);
        }
    }

    private static ByFieldMapsDeviceAggregateRow ToFieldMapsRow(DeviceTestRow row) => new()
    {
        LocalDate = row.LocalDate,
        DeviceId = row.DeviceId,
        Username = row.Username,
        TimeTakenSecond = row.TimeTakenSecond,
        Hits = row.Hits,
    };

    private static BySurvey123DeviceAggregateRow ToSurvey123Row(DeviceTestRow row) => new()
    {
        LocalDate = row.LocalDate,
        DeviceId = row.DeviceId,
        Username = row.Username,
        TimeTakenSecond = row.TimeTakenSecond,
        Hits = row.Hits,
    };
}
