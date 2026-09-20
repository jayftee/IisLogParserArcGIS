using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Domain.Aggregation;

namespace IisLogParserArcGIS.Data.Repositories;

/// <summary>
/// CRUD for the <see cref="AggregateTableNames.ByUserAgent"/> table, per the pattern established by
/// <see cref="AggregateRepositoryBase{TRow}"/>.
/// </summary>
public sealed class ByUserAgentRepository : AggregateRepositoryBase<ByUserAgentAggregateRow>
{
    /// <inheritdoc/>
    protected override string InsertSql =>
        $"""
        INSERT INTO {AggregateTableNames.ByUserAgent} (local_date, user_agent, time_taken_second, hits)
        VALUES (@LocalDate, @UserAgent, @TimeTakenSecond, @Hits)
        """;

    /// <inheritdoc/>
    protected override string DeleteByLocalDateSql =>
        $"DELETE FROM {AggregateTableNames.ByUserAgent} WHERE local_date = @LocalDate";

    /// <inheritdoc/>
    protected override string SelectByLocalDateSql =>
        $"""
        SELECT local_date AS LocalDate, user_agent AS UserAgent, time_taken_second AS TimeTakenSecond, hits AS Hits
        FROM {AggregateTableNames.ByUserAgent}
        WHERE local_date = @LocalDate
        """;
}
