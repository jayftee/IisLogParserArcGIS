using System.Data;

namespace IisLogParserArcGIS.Data.Queries;

/// <summary>
/// Base for non-CRUD, cross-cutting/reporting queries, kept outside any single-table
/// <see cref="Repositories.AggregateRepositoryBase{TRow}"/> repository per ADR 0002 - this is where future
/// queries that span multiple aggregate tables or don't fit the replace-one-table CRUD shape belong.
/// </summary>
public abstract class AggregateQueryBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateQueryBase"/> class.
    /// </summary>
    /// <param name="connection">The database connection to query against.</param>
    protected AggregateQueryBase(IDbConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        Connection = connection;
    }

    /// <summary>
    /// Gets the database connection to query against.
    /// </summary>
    protected IDbConnection Connection { get; }
}
