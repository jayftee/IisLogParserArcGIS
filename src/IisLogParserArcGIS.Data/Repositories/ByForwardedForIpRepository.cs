using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Domain.Aggregation;

namespace IisLogParserArcGIS.Data.Repositories;

/// <summary>
/// CRUD for the <see cref="AggregateTableNames.ByForwardedForIp"/> table, per the pattern established by
/// <see cref="AggregateRepositoryBase{TRow}"/>.
/// </summary>
public sealed class ByForwardedForIpRepository : AggregateRepositoryBase<ByForwardedForIpAggregateRow>
{
    /// <inheritdoc/>
    protected override string InsertSql =>
        $"""
        INSERT INTO {AggregateTableNames.ByForwardedForIp} (local_date, forwarded_for_ip, time_taken_second, hits)
        VALUES (@LocalDate, @ForwardedForIp, @TimeTakenSecond, @Hits)
        """;

    /// <inheritdoc/>
    protected override string DeleteByLocalDateSql =>
        $"DELETE FROM {AggregateTableNames.ByForwardedForIp} WHERE local_date = @LocalDate";

    /// <inheritdoc/>
    protected override string SelectByLocalDateSql =>
        $"""
        SELECT local_date AS LocalDate, forwarded_for_ip AS ForwardedForIp, time_taken_second AS TimeTakenSecond, hits AS Hits
        FROM {AggregateTableNames.ByForwardedForIp}
        WHERE local_date = @LocalDate
        """;
}
