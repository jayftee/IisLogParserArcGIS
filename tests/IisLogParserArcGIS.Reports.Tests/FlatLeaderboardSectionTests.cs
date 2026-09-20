using System.Text.Json;
using Dapper;
using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Data.Repositories;
using IisLogParserArcGIS.Domain.Aggregation;
using IisLogParserArcGIS.Reports.Configuration;
using IisLogParserArcGIS.Reports.Tests.TestSupport;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Time.Testing;

namespace IisLogParserArcGIS.Reports.Tests;

/// <summary>
/// Verifies the flat Leaderboard section (ticket 17) end-to-end against the real, harvested fixture database
/// (ticket 09), per spec.md's "primary seam" testing decision: drive <see cref="RegenerationRun.Run"/> and assert
/// against the files it writes, rather than against internal query/render classes directly.
/// </summary>
[Collection(HarvestedAggregateDatabaseCollection.Name)]
public sealed class FlatLeaderboardSectionTests
{
    // The harvested fixture's corpus runs 2026-01-01 through 2026-09-10; pinning "today" to the corpus's last
    // date makes the All (year-to-date) range fall entirely within real harvested data.
    private static readonly DateOnly _today = new(2026, 9, 10);
    private static readonly DateOnly _yearStart = new(2026, 1, 1);

    private readonly HarvestedAggregateDatabaseFixture _fixture;

    public FlatLeaderboardSectionTests(HarvestedAggregateDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public static IEnumerable<object[]> DimensionPages()
    {
        yield return ["leaderboard/forwarded-for-ip/all.html"];
        yield return ["leaderboard/forwarded-for-ip/last-7-days.html"];
        yield return ["leaderboard/referer/all.html"];
        yield return ["leaderboard/referer/last-7-days.html"];
        yield return ["leaderboard/uri/all.html"];
        yield return ["leaderboard/uri/last-7-days.html"];
        yield return ["leaderboard/user-agent/all.html"];
        yield return ["leaderboard/user-agent/last-7-days.html"];
    }

    [Theory]
    [MemberData(nameof(DimensionPages))]
    public void Run_WritesEveryLeaderboardPage(string relativePath)
    {
        ArgumentNullException.ThrowIfNull(relativePath);

        using var outputDirectory = new TempDirectoryPath();
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        Assert.True(File.Exists(ToFullPath(outputDirectory.Path, relativePath)));
    }

    [Theory]
    [MemberData(nameof(DimensionPages))]
    public void Run_SidebarOnEachLeaderboardPage_MarksItsOwnEntryActive(string relativePath)
    {
        ArgumentNullException.ThrowIfNull(relativePath);

        using var outputDirectory = new TempDirectoryPath();
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        var html = File.ReadAllText(ToFullPath(outputDirectory.Path, relativePath));

        Assert.Contains($"{relativePath}\" class=\"active\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_ForwardedForIpLeaderboard_TurnsOnBuiltInPaging()
    {
        using var outputDirectory = new TempDirectoryPath();
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        var html = File.ReadAllText(ToFullPath(outputDirectory.Path, "leaderboard/forwarded-for-ip/all.html"));

        Assert.Contains("page: 'enable'", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_ForwardedForIpLeaderboard_MatchesAnIndependentlyComputedOracle()
    {
        using var outputDirectory = new TempDirectoryPath();
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        var html = File.ReadAllText(ToFullPath(outputDirectory.Path, "leaderboard/forwarded-for-ip/all.html"));
        var oracleRows = QueryOracle(connection, new FlatAggregateColumn("aggregated_by_forwarded_for_ip", "forwarded_for_ip"));

        AssertMatchesOracle(html, oracleRows);
    }

    [Fact]
    public void Run_UriLeaderboard_MatchesAnIndependentlyComputedOracle()
    {
        using var outputDirectory = new TempDirectoryPath();
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        var html = File.ReadAllText(ToFullPath(outputDirectory.Path, "leaderboard/uri/all.html"));
        var oracleRows = QueryOracle(connection, new FlatAggregateColumn("aggregated_by_uri", "uri_stem"));

        AssertMatchesOracle(html, oracleRows);
    }

    [Fact]
    public void Run_UserAgentLeaderboard_MatchesAnIndependentlyComputedOracle()
    {
        using var outputDirectory = new TempDirectoryPath();
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        var html = File.ReadAllText(ToFullPath(outputDirectory.Path, "leaderboard/user-agent/all.html"));
        var oracleRows = QueryOracle(connection, new FlatAggregateColumn("aggregated_by_user_agent", "user_agent"));

        AssertMatchesOracle(html, oracleRows);
    }

    [Fact]
    public void Run_RefererLeaderboard_MatchesAnIndependentlyComputedOracle_ExcludingTheEmptyPlaceholder()
    {
        using var outputDirectory = new TempDirectoryPath();
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        var html = File.ReadAllText(ToFullPath(outputDirectory.Path, "leaderboard/referer/all.html"));
        var oracleRows = QueryOracle(connection, new FlatAggregateColumn("aggregated_by_referer", "referer", "referer <> '-'"));

        AssertMatchesOracle(html, oracleRows);
    }

    [Fact]
    public void Run_RefererLeaderboard_NeverListsTheEmptyPlaceholderAsARow()
    {
        using var outputDirectory = new TempDirectoryPath();
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);

        var placeholderCount = connection.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM aggregated_by_referer WHERE referer = '-' AND local_date BETWEEN @Start AND @End",
            new { Start = _yearStart, End = _today });
        Assert.True(placeholderCount > 0, "Expected the harvested fixture to contain at least one '-' referer row for this test to be meaningful.");

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        var html = File.ReadAllText(ToFullPath(outputDirectory.Path, "leaderboard/referer/all.html"));
        var dataRows = ExtractDataRows(html);

        Assert.DoesNotContain(dataRows, row => row[0].GetString() == "-");
    }

    [Fact]
    public void Run_ForwardedForIpLeaderboard_WithMoreThanFiveHundredDistinctIps_CapsAtFiveHundredRankedDescending()
    {
        using var workingCopy = new WorkingDatabaseCopy(_fixture.DatabasePath);
        const int DistinctIpCount = 505;
        const int HitsBase = 1_000_000;
        using (var seedConnection = workingCopy.OpenConnection())
        {
            new ByForwardedForIpRepository().Insert(
                seedConnection,
                Enumerable.Range(0, DistinctIpCount).Select(index => new ByForwardedForIpAggregateRow
                {
                    LocalDate = _today,
                    ForwardedForIp = $"198.51.100.{index}",
                    TimeTakenSecond = 1.0,
                    Hits = HitsBase - index,
                }));
        }

        using var outputDirectory = new TempDirectoryPath();
        using var connection = workingCopy.OpenConnection();

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        var html = File.ReadAllText(ToFullPath(outputDirectory.Path, "leaderboard/forwarded-for-ip/all.html"));
        var dataRows = ExtractDataRows(html);

        Assert.Equal(500, dataRows.Length);
        Assert.Equal("198.51.100.0", dataRows[0][0].GetString());
        Assert.Equal(HitsBase, dataRows[0][1].GetInt32());
        Assert.Equal("198.51.100.499", dataRows[^1][0].GetString());
        Assert.Equal(HitsBase - 499, dataRows[^1][1].GetInt32());
    }

    private static string ToFullPath(string outputDirectory, string relativePath)
    {
        return Path.Combine(outputDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private static (string Value, int Hits)[] QueryOracle(SqliteConnection connection, FlatAggregateColumn source)
    {
        var whereClause = source.ExtraWhere is null ? string.Empty : $"AND {source.ExtraWhere}";
        var sql = $"""
            SELECT {source.Column} AS Value, SUM(hits) AS Hits
            FROM {source.Table}
            WHERE local_date BETWEEN @Start AND @End {whereClause}
            GROUP BY {source.Column}
            ORDER BY Hits DESC
            LIMIT 500
            """;

        return connection.Query<(string Value, int Hits)>(sql, new { Start = _yearStart, End = _today }).ToArray();
    }

    private static void AssertMatchesOracle(string html, (string Value, int Hits)[] oracleRows)
    {
        Assert.NotEmpty(oracleRows);
        var dataRows = ExtractDataRows(html);

        Assert.Equal(oracleRows.Length, dataRows.Length);

        for (var index = 0; index < oracleRows.Length; index++)
        {
            Assert.Equal(oracleRows[index].Value, dataRows[index][0].GetString());
            Assert.Equal(oracleRows[index].Hits, dataRows[index][1].GetInt32());
        }
    }

    private static JsonElement[] ExtractDataRows(string html)
    {
        const string Marker = "google.visualization.arrayToDataTable(";
        var start = html.IndexOf(Marker, StringComparison.Ordinal) + Marker.Length;
        var end = FindMatchingArrayEnd(html, start);

        return JsonDocument.Parse(html[start..end]).RootElement.EnumerateArray().Skip(1).ToArray();
    }

    // A plain IndexOf(");") search for the array's end breaks whenever a cell value (a real-world user agent,
    // say) itself contains the literal characters ");" - this walks the actual bracket/brace nesting instead,
    // skipping over string content (escapes included) so it can't be fooled by what a string happens to contain.
    private static int FindMatchingArrayEnd(string json, int start)
    {
        var depth = 0;
        var insideString = false;
        var escaped = false;

        for (var index = start; index < json.Length; index++)
        {
            var character = json[index];

            if (insideString)
            {
                if (escaped)
                {
                    escaped = false;
                }
                else if (character == '\\')
                {
                    escaped = true;
                }
                else if (character == '"')
                {
                    insideString = false;
                }

                continue;
            }

            if (character == '"')
            {
                insideString = true;
            }
            else if (character is '[' or '{')
            {
                depth++;
            }
            else if (character is ']' or '}')
            {
                depth--;

                if (depth == 0)
                {
                    return index + 1;
                }
            }
        }

        throw new InvalidOperationException("Could not find the end of the embedded Google Charts data table array.");
    }

    private static FakeTimeProvider CreateTimeProvider()
    {
        return new FakeTimeProvider(new DateTimeOffset(_today, TimeOnly.MinValue, TimeSpan.Zero));
    }

    private static ReportsSettings CreateSettings()
    {
        return new ReportsSettings
        {
            LocalTimeZone = "UTC",
            IncludedRoots =
            [
                "arcgis", "charon", "deimos", "europa", "galatea", "iapetus", "kerberos",
                "mimas", "namaka", "oberon", "phobos", "rhea", "titan", "umbriel",
            ],
            PortalBaseUrl = "portal.example.com",
            OutputDirectory = "Dashboard",
        };
    }

    private sealed record FlatAggregateColumn(string Table, string Column, string? ExtraWhere = null);
}
