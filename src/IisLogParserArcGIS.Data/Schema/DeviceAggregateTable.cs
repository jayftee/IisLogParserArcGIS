namespace IisLogParserArcGIS.Data.Schema;

/// <summary>
/// One of the two by-device aggregate tables (Field Maps or Survey123). The per-device queries splice a table's
/// name into their SQL text rather than bind it as a parameter, so they take this type, which has exactly two
/// instances and no public constructor, instead of a free-form string: no caller can hand them an arbitrary table
/// name.
/// </summary>
public sealed class DeviceAggregateTable
{
    private DeviceAggregateTable(string name)
    {
        Name = name;
    }

#pragma warning disable CC0309 // "Field Maps" is the Esri product name, not a log field.
    /// <summary>
    /// Gets the by-Field-Maps-device table.
    /// </summary>
    public static DeviceAggregateTable FieldMaps { get; } = new(AggregateTableNames.ByFieldMapsDevice);
#pragma warning restore CC0309

    /// <summary>
    /// Gets the by-Survey123-device table.
    /// </summary>
    public static DeviceAggregateTable Survey123 { get; } = new(AggregateTableNames.BySurvey123Device);

    /// <summary>
    /// Gets the table's name, one of the <see cref="AggregateTableNames"/> constants.
    /// </summary>
    public string Name { get; }
}
