using System.Data;
using Dapper;

namespace IisLogParserArcGIS.Data.Schema;

/// <summary>
/// Creates the ten aggregate tables idempotently via <c>CREATE TABLE IF NOT EXISTS</c>, so a fresh database
/// file is usable immediately and re-running the program against an existing database file never errors on
/// schema creation.
/// </summary>
public static class AggregateDatabaseSchema
{
    private const string CreateTablesSql = """
        CREATE TABLE IF NOT EXISTS aggregated_by_uri (
            id INTEGER PRIMARY KEY,
            local_date TEXT NOT NULL,
            uri_stem TEXT NOT NULL,
            time_taken_second REAL NOT NULL,
            hits INTEGER NOT NULL
        );

        CREATE TABLE IF NOT EXISTS aggregated_by_root (
            id INTEGER PRIMARY KEY,
            local_date TEXT NOT NULL,
            root TEXT NOT NULL,
            time_taken_second REAL NOT NULL,
            hits INTEGER NOT NULL
        );

        CREATE TABLE IF NOT EXISTS aggregated_by_user_agent (
            id INTEGER PRIMARY KEY,
            local_date TEXT NOT NULL,
            user_agent TEXT NOT NULL,
            time_taken_second REAL NOT NULL,
            hits INTEGER NOT NULL
        );

        CREATE TABLE IF NOT EXISTS aggregated_by_referer (
            id INTEGER PRIMARY KEY,
            local_date TEXT NOT NULL,
            referer TEXT NOT NULL,
            time_taken_second REAL NOT NULL,
            hits INTEGER NOT NULL
        );

        CREATE TABLE IF NOT EXISTS aggregated_by_forwarded_for_ip (
            id INTEGER PRIMARY KEY,
            local_date TEXT NOT NULL,
            forwarded_for_ip TEXT NOT NULL,
            time_taken_second REAL NOT NULL,
            hits INTEGER NOT NULL
        );

        CREATE TABLE IF NOT EXISTS aggregated_by_referer_and_uri (
            id INTEGER PRIMARY KEY,
            local_date TEXT NOT NULL,
            referer TEXT NOT NULL,
            uri_stem TEXT NOT NULL,
            time_taken_second REAL NOT NULL,
            hits INTEGER NOT NULL
        );

        CREATE TABLE IF NOT EXISTS aggregated_by_arcgis_service (
            id INTEGER PRIMARY KEY,
            local_date TEXT NOT NULL,
            site TEXT NOT NULL,
            folder TEXT NULL,
            service_name TEXT NOT NULL,
            service_type TEXT NOT NULL,
            successful_time_taken_second REAL NOT NULL,
            failed_time_taken_second REAL NOT NULL,
            hits INTEGER NOT NULL,
            successful_hits INTEGER NOT NULL,
            failed_hits INTEGER NOT NULL
        );

        CREATE TABLE IF NOT EXISTS aggregated_by_portal_item (
            id INTEGER PRIMARY KEY,
            local_date TEXT NOT NULL,
            portal_item_id TEXT NOT NULL,
            time_taken_second REAL NOT NULL,
            hits INTEGER NOT NULL,
            successful_hits INTEGER NOT NULL,
            failed_hits INTEGER NOT NULL
        );

        CREATE TABLE IF NOT EXISTS aggregated_by_field_maps_device (
            id INTEGER PRIMARY KEY,
            local_date TEXT NOT NULL,
            device_id TEXT NOT NULL,
            username TEXT NULL,
            time_taken_second REAL NOT NULL,
            hits INTEGER NOT NULL
        );

        CREATE INDEX IF NOT EXISTS ix_field_maps_device_device_id ON aggregated_by_field_maps_device (device_id);

        CREATE TABLE IF NOT EXISTS aggregated_by_survey123_device (
            id INTEGER PRIMARY KEY,
            local_date TEXT NOT NULL,
            device_id TEXT NOT NULL,
            username TEXT NULL,
            time_taken_second REAL NOT NULL,
            hits INTEGER NOT NULL
        );

        CREATE INDEX IF NOT EXISTS ix_survey123_device_device_id ON aggregated_by_survey123_device (device_id);
        """;

    /// <summary>
    /// Creates every aggregate table that does not already exist on <paramref name="connection"/>.
    /// </summary>
    /// <param name="connection">An open connection to the output aggregate database.</param>
    public static void EnsureCreated(IDbConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        connection.Execute(CreateTablesSql);
    }

    /// <summary>
    /// Reports whether <paramref name="tableName"/> exists on <paramref name="connection"/> - for a read-only
    /// consumer (the Dashboard) that must degrade gracefully against a database file created before a table
    /// was introduced, rather than run <see cref="EnsureCreated"/> and write to it.
    /// </summary>
    /// <param name="connection">An open connection to the aggregate database.</param>
    /// <param name="tableName">The table name, e.g. one of <see cref="AggregateTableNames"/>.</param>
    /// <returns><see langword="true"/> when the table exists.</returns>
    public static bool TableExists(IDbConnection connection, string tableName)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(tableName);

        return connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = @TableName",
            new { TableName = tableName }) > 0;
    }
}
