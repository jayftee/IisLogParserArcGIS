using System.Data;
using Dapper;

namespace IisLogParserArcGIS.Data.Repositories;

/// <summary>
/// The persistence shared by the by-device aggregate tables (Field Maps and Survey123): identical columns
/// (<c>local_date</c>, <c>device_id</c>, nullable <c>username</c>, <c>time_taken_second</c>, <c>hits</c>) and the
/// same cross-date username back-fill (tickets 20 and 21), differing only in the table.
/// </summary>
/// <typeparam name="TRow">The aggregate row type this repository persists.</typeparam>
public abstract class DeviceAggregateRepositoryBase<TRow> : AggregateRepositoryBase<TRow>
{
    /// <summary>
    /// Gets the name of the by-device table this repository persists to.
    /// </summary>
    protected abstract string TableName { get; }

    /// <inheritdoc/>
    protected override string InsertSql =>
        $"""
        INSERT INTO {TableName}
            (local_date, device_id, username, time_taken_second, hits)
        VALUES
            (@LocalDate, @DeviceId, @Username, @TimeTakenSecond, @Hits)
        """;

    /// <inheritdoc/>
    protected override string DeleteByLocalDateSql =>
        $"DELETE FROM {TableName} WHERE local_date = @LocalDate";

    /// <inheritdoc/>
    protected override string SelectByLocalDateSql =>
        $"""
        SELECT local_date AS LocalDate, device_id AS DeviceId, username AS Username,
            time_taken_second AS TimeTakenSecond, hits AS Hits
        FROM {TableName}
        WHERE local_date = @LocalDate
        """;

    /// <summary>
    /// Back-fills <c>username</c> on every row of <paramref name="tableName"/>, across all local dates, that
    /// still has none: each such row takes the username of the first row (earliest <c>local_date</c>, ties
    /// broken alphabetically) of the same device that has one. A device with no username anywhere is left
    /// untouched, and a row that already has a username is never overwritten, so the operation is idempotent and
    /// independent of the order dates were harvested in. The first username per device is computed once, up
    /// front, rather than once per empty row, which keeps the cost roughly linear in the table size (a per-row
    /// lookup took about 12 seconds at 650k rows).
    /// </summary>
    /// <param name="connection">An open database connection.</param>
    /// <param name="tableName">The by-device table to back-fill, one of the <c>AggregateTableNames</c> constants.</param>
    /// <param name="transaction">The transaction to enlist in, if any.</param>
    /// <returns>The number of rows that were given a username.</returns>
    protected static int BackfillUsernames(IDbConnection connection, string tableName, IDbTransaction? transaction)
    {
        ArgumentNullException.ThrowIfNull(connection);

        var sql = $"""
            WITH donor AS MATERIALIZED (
                SELECT device_id, username
                FROM (
                    SELECT device_id, username,
                        ROW_NUMBER() OVER (PARTITION BY device_id ORDER BY local_date, username) AS donor_rank
                    FROM {tableName}
                    WHERE username IS NOT NULL)
                WHERE donor_rank = 1)
            UPDATE {tableName}
            SET username = (
                SELECT donor.username
                FROM donor
                WHERE donor.device_id = {tableName}.device_id)
            WHERE username IS NULL
                AND device_id IN (SELECT device_id FROM donor)
            """;

        return connection.Execute(sql, transaction: transaction);
    }
}
