using Dapper;
using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Data.Tests.TestSupport;

namespace IisLogParserArcGIS.Data.Tests.Schema;

public class AggregateDatabaseSchemaTests
{
    [Fact]
    public void EnsureCreated_CreatesAllTenAggregateTables()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);

        AggregateDatabaseSchema.EnsureCreated(connection);

        var tableNames = connection.Query<string>("SELECT name FROM sqlite_master WHERE type = 'table'").ToArray();

        Assert.All(AggregateTableNames.All, tableName => Assert.Contains(tableName, tableNames));
    }

    [Fact]
    public void EnsureCreated_CreatesTheFieldMapsDeviceTableWithANullableUsernameAndADeviceIdIndex()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);

        AggregateDatabaseSchema.EnsureCreated(connection);

        var usernameIsNullable = connection.ExecuteScalar<int>(
            $"SELECT \"notnull\" FROM pragma_table_info('{AggregateTableNames.ByFieldMapsDevice}') WHERE name = 'username'");
        var indexNames = connection.Query<string>($"SELECT name FROM pragma_index_list('{AggregateTableNames.ByFieldMapsDevice}')").ToArray();
        Assert.Equal(0, usernameIsNullable);
        Assert.Contains("ix_field_maps_device_device_id", indexNames);
    }

    [Fact]
    public void EnsureCreated_CreatesTheSurvey123DeviceTableWithANullableUsernameAndADeviceIdIndex()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);

        AggregateDatabaseSchema.EnsureCreated(connection);

        var usernameIsNullable = connection.ExecuteScalar<int>(
            $"SELECT \"notnull\" FROM pragma_table_info('{AggregateTableNames.BySurvey123Device}') WHERE name = 'username'");
        var indexNames = connection.Query<string>($"SELECT name FROM pragma_index_list('{AggregateTableNames.BySurvey123Device}')").ToArray();
        Assert.Equal(0, usernameIsNullable);
        Assert.Contains("ix_survey123_device_device_id", indexNames);
    }

    [Fact]
    public void EnsureCreated_RegistersTenAggregateTables()
    {
        Assert.Equal(10, AggregateTableNames.All.Count);
    }

    [Fact]
    public void TableExists_IsFalseBeforeEnsureCreated_AndTrueAfter()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);

        Assert.False(AggregateDatabaseSchema.TableExists(connection, AggregateTableNames.ByFieldMapsDevice));

        AggregateDatabaseSchema.EnsureCreated(connection);

        Assert.True(AggregateDatabaseSchema.TableExists(connection, AggregateTableNames.ByFieldMapsDevice));
        Assert.False(AggregateDatabaseSchema.TableExists(connection, "no_such_table"));
    }

    [Fact]
    public void EnsureCreated_CalledTwiceAgainstTheSameDatabaseFile_DoesNotThrow()
    {
        using var database = new TempSqliteDatabase();
        using var firstConnection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(firstConnection);
        firstConnection.Close();

        using var secondConnection = SqliteConnectionFactory.Open(database.Path);
        var exception = Record.Exception(() => AggregateDatabaseSchema.EnsureCreated(secondConnection));

        Assert.Null(exception);
    }

    [Fact]
    public void EnsureCreated_CalledTwice_LeavesPreviouslyInsertedRowsIntact()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        connection.Execute(
            $"INSERT INTO {AggregateTableNames.ByUri} (local_date, uri_stem, time_taken_second, hits) VALUES ('2026-05-01', '/foo', 1.5, 3)");

        AggregateDatabaseSchema.EnsureCreated(connection);

        var hits = connection.ExecuteScalar<int>($"SELECT hits FROM {AggregateTableNames.ByUri} WHERE uri_stem = '/foo'");
        Assert.Equal(3, hits);
    }

    [Fact]
    public void EnsureCreated_WithNullConnection_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => AggregateDatabaseSchema.EnsureCreated(null!));
    }
}
