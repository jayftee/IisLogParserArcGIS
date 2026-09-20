using System.Data;
using Dapper;

namespace IisLogParserArcGIS.Data.Repositories;

/// <summary>
/// Base CRUD operations scoped to a single aggregate table, per ADR 0002. One concrete repository per
/// aggregate table derives from this class and supplies its own hand-written SQL text for each operation -
/// the schema is fixed and replace-oriented, so Dapper executes explicit SQL rather than going through an
/// ORM/change-tracker.
/// </summary>
/// <typeparam name="TRow">The aggregate row type this repository persists.</typeparam>
public abstract class AggregateRepositoryBase<TRow>
{
    /// <summary>
    /// Gets the SQL text that inserts one or more <typeparamref name="TRow"/> rows.
    /// </summary>
    protected abstract string InsertSql { get; }

    /// <summary>
    /// Gets the SQL text that deletes every row for a given <c>local_date</c>. The query must bind a
    /// <c>@LocalDate</c> parameter.
    /// </summary>
    protected abstract string DeleteByLocalDateSql { get; }

    /// <summary>
    /// Gets the SQL text that selects every row for a given <c>local_date</c>. The query must bind a
    /// <c>@LocalDate</c> parameter and project columns onto <typeparamref name="TRow"/>'s members.
    /// </summary>
    protected abstract string SelectByLocalDateSql { get; }

    /// <summary>
    /// Inserts <paramref name="rows"/>.
    /// </summary>
    /// <param name="connection">An open database connection.</param>
    /// <param name="rows">The rows to insert.</param>
    /// <param name="transaction">The transaction to enlist in, if any.</param>
    public void Insert(IDbConnection connection, IEnumerable<TRow> rows, IDbTransaction? transaction = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(rows);

        connection.Execute(InsertSql, rows, transaction);
    }

    /// <summary>
    /// Deletes every row for <paramref name="localDate"/>.
    /// </summary>
    /// <param name="connection">An open database connection.</param>
    /// <param name="localDate">The local date whose rows should be deleted.</param>
    /// <param name="transaction">The transaction to enlist in, if any.</param>
    /// <returns>The number of rows deleted.</returns>
    public int DeleteByLocalDate(IDbConnection connection, DateOnly localDate, IDbTransaction? transaction = null)
    {
        ArgumentNullException.ThrowIfNull(connection);

        return connection.Execute(DeleteByLocalDateSql, new { LocalDate = localDate }, transaction);
    }

    /// <summary>
    /// Selects every row for <paramref name="localDate"/>.
    /// </summary>
    /// <param name="connection">An open database connection.</param>
    /// <param name="localDate">The local date whose rows should be selected.</param>
    /// <returns>The matching rows.</returns>
    public IEnumerable<TRow> GetByLocalDate(IDbConnection connection, DateOnly localDate)
    {
        ArgumentNullException.ThrowIfNull(connection);

        return connection.Query<TRow>(SelectByLocalDateSql, new { LocalDate = localDate });
    }
}
