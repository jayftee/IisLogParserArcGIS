namespace IisLogParserArcGIS.Domain.Parsing;

/// <summary>
/// Maps field names declared on one <c>#Fields</c> header line to their column position, so data lines governed
/// by that header can be resolved by name rather than by a hard-coded position.
/// </summary>
public sealed class LogFieldIndex
{
    private readonly Dictionary<string, int> _columnIndexes;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogFieldIndex"/> class.
    /// </summary>
    /// <param name="fieldNames">The field names, in declared order, from a <c>#Fields</c> header line.</param>
    public LogFieldIndex(IReadOnlyList<string> fieldNames)
    {
        ArgumentNullException.ThrowIfNull(fieldNames);

        FieldCount = fieldNames.Count;
        _columnIndexes = new Dictionary<string, int>(fieldNames.Count, StringComparer.OrdinalIgnoreCase);

        for (var columnIndex = 0; columnIndex < fieldNames.Count; columnIndex++)
        {
            _columnIndexes[fieldNames[columnIndex]] = columnIndex;
        }
    }

    /// <summary>
    /// Gets the number of fields declared on the governing header line.
    /// </summary>
    public int FieldCount { get; }

    /// <summary>
    /// Determines whether the governing header line declared the given field name.
    /// </summary>
    /// <param name="fieldName">The field name to look for.</param>
    /// <returns><see langword="true"/> when the field is declared; otherwise <see langword="false"/>.</returns>
    public bool HasField(string fieldName)
    {
        ArgumentNullException.ThrowIfNull(fieldName);

        return _columnIndexes.ContainsKey(fieldName);
    }

    /// <summary>
    /// Resolves the value of the given field name from a data line's whitespace-split fields.
    /// </summary>
    /// <param name="rowFields">One data line's fields, split in the same order as the header.</param>
    /// <param name="fieldName">The field name to resolve, as declared on the governing header line.</param>
    /// <returns>The resolved field value.</returns>
    public string GetValue(IReadOnlyList<string> rowFields, string fieldName)
    {
        ArgumentNullException.ThrowIfNull(rowFields);
        ArgumentNullException.ThrowIfNull(fieldName);

        return rowFields[_columnIndexes[fieldName]];
    }
}
