using System.Data;
using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Domain.Aggregation;

namespace IisLogParserArcGIS.Data.Repositories;

/// <summary>
/// CRUD for the <see cref="AggregateTableNames.ByFieldMapsDevice"/> table, per the pattern established by
/// <see cref="AggregateRepositoryBase{TRow}"/>.
/// </summary>
public sealed class ByFieldMapsDeviceRepository : DeviceAggregateRepositoryBase<ByFieldMapsDeviceAggregateRow>
{
    /// <inheritdoc/>
    protected override string TableName => AggregateTableNames.ByFieldMapsDevice;

    /// <summary>
    /// Back-fills <c>username</c> on every Field Maps row, across all local dates, that still has none, from the
    /// first username (earliest <c>local_date</c>, ties alphabetical) of the same device. See
    /// <see cref="DeviceAggregateRepositoryBase{TRow}"/> for the rule and its properties.
    /// </summary>
    /// <param name="connection">An open database connection.</param>
    /// <param name="transaction">The transaction to enlist in, if any.</param>
    /// <returns>The number of rows that were given a username.</returns>
    public static int BackfillUsernames(IDbConnection connection, IDbTransaction? transaction = null) =>
        BackfillUsernames(connection, AggregateTableNames.ByFieldMapsDevice, transaction);
}
