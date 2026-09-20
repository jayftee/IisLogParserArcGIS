using System.Data;
using IisLogParserArcGIS.Data.Repositories;

namespace IisLogParserArcGIS.Data.Replacement;

/// <summary>
/// Replaces a target local date's rows across every one of the ten aggregate tables as one atomic
/// transaction: delete that date's existing rows, then insert the freshly computed rows, committed together.
/// Re-running a date is therefore always a full replace rather than an append, and a failure partway through
/// leaves every table exactly as it was before the run started. The one exception to "only the target date's
/// rows change" is the Field Maps and Survey123 username back-fills (tickets 20 and 21), which run after their
/// table's inserts, in the same transaction, and may fill in still-empty usernames on other dates' rows.
/// </summary>
public static class DailyAggregateReplacer
{
    /// <summary>
    /// Replaces <paramref name="batch"/>'s target local date's rows in every aggregate table, as one
    /// transaction on <paramref name="connection"/>. If any delete, insert, or a username back-fill
    /// throws, the transaction is rolled back and none of the ten tables retain any change from this call.
    /// </summary>
    /// <param name="connection">An open database connection.</param>
    /// <param name="batch">The freshly computed rows, and the local date they were aggregated for.</param>
    /// <param name="beforeCommit">
    /// An optional hook invoked with <paramref name="connection"/> after every delete/insert has run but
    /// before the transaction commits. Exists for tests to verify that a concurrent connection cannot observe
    /// this call's changes until it commits; production callers should omit it.
    /// </param>
    public static void Replace(IDbConnection connection, DailyAggregateBatch batch, Action<IDbConnection>? beforeCommit = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(batch);

        using var transaction = connection.BeginTransaction();
        try
        {
            void ReplaceTable<TRow>(AggregateRepositoryBase<TRow> repository, IEnumerable<TRow> rows)
            {
                repository.DeleteByLocalDate(connection, batch.LocalDate, transaction);
                repository.Insert(connection, rows, transaction);
            }

            ReplaceTable(new ByUriRepository(), batch.ByUri);
            ReplaceTable(new ByRootRepository(), batch.ByRoot);
            ReplaceTable(new ByUserAgentRepository(), batch.ByUserAgent);
            ReplaceTable(new ByRefererRepository(), batch.ByReferer);
            ReplaceTable(new ByForwardedForIpRepository(), batch.ByForwardedForIp);
            ReplaceTable(new ByRefererAndUriRepository(), batch.ByRefererAndUri);
            ReplaceTable(new ByArcGisServiceRepository(), batch.ByArcGisService);
            ReplaceTable(new ByPortalItemRepository(), batch.ByPortalItem);
            ReplaceTable(new ByFieldMapsDeviceRepository(), batch.ByFieldMapsDevice);
            ByFieldMapsDeviceRepository.BackfillUsernames(connection, transaction);
            ReplaceTable(new BySurvey123DeviceRepository(), batch.BySurvey123Device);
            BySurvey123DeviceRepository.BackfillUsernames(connection, transaction);

            beforeCommit?.Invoke(connection);

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
