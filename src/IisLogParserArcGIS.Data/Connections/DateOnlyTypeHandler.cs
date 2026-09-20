using System.Data;
using System.Globalization;
using Dapper;

namespace IisLogParserArcGIS.Data.Connections;

/// <summary>
/// Maps <see cref="DateOnly"/> to/from the <c>yyyy-MM-dd</c> text SQLite stores <c>local_date</c> columns as.
/// Dapper has no built-in <see cref="DateOnly"/> support, so this handler must be registered (see
/// <see cref="SqliteConnectionFactory"/>'s static constructor) before any query using it runs.
/// </summary>
internal sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    private const string DateFormat = "yyyy-MM-dd";

    /// <inheritdoc/>
    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        ArgumentNullException.ThrowIfNull(parameter);

        parameter.Value = value.ToString(DateFormat, CultureInfo.InvariantCulture);
    }

    /// <inheritdoc/>
    public override DateOnly Parse(object value)
    {
        return DateOnly.ParseExact((string)value, DateFormat, CultureInfo.InvariantCulture);
    }
}
