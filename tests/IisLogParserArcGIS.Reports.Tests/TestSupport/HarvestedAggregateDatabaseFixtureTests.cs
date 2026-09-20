using Dapper;
using IisLogParserArcGIS.Data.Connections;

namespace IisLogParserArcGIS.Reports.Tests.TestSupport;

/// <summary>
/// Proves <see cref="HarvestedAggregateDatabaseFixture"/> actually delivers what later Reports tickets' tests
/// will rely on: the real Included Roots, the <c>proxy</c> Root, and a realistic spread of ArcGIS Server
/// services and Portal Items - not just that the fixture runs without error.
/// </summary>
[Collection(HarvestedAggregateDatabaseCollection.Name)]
public sealed class HarvestedAggregateDatabaseFixtureTests
{
    // The real 14 Included Roots, per ticket 04's resolution.
    private static readonly string[] _realIncludedRoots =
    [
        "arcgis", "charon", "deimos", "europa", "galatea", "iapetus", "kerberos",
        "mimas", "namaka", "oberon", "phobos", "rhea", "titan", "umbriel",
    ];

    private readonly HarvestedAggregateDatabaseFixture _fixture;

    public HarvestedAggregateDatabaseFixtureTests(HarvestedAggregateDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void DatabasePath_ContainsEveryRealIncludedRoot()
    {
        var roots = QueryDistinctRoots();

        foreach (var includedRoot in _realIncludedRoots)
        {
            Assert.Contains(includedRoot, roots);
        }
    }

    [Fact]
    public void DatabasePath_ContainsTheProxyRoot()
    {
        var roots = QueryDistinctRoots();

        Assert.Contains("proxy", roots);
    }

    // The full corpus yields 917 distinct services and 1,079 distinct Portal Items; a much lower floor still
    // distinguishes a realistic spread from a grouping-key regression that collapses almost everything.
    [Fact]
    public void DatabasePath_ContainsARealisticSpreadOfArcGisServicesAndPortalItems()
    {
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);

        var serviceCount = connection.QuerySingle<int>(
            "SELECT COUNT(DISTINCT site || '/' || COALESCE(folder, '') || '/' || service_name) FROM aggregated_by_arcgis_service");
        var portalItemCount = connection.QuerySingle<int>(
            "SELECT COUNT(DISTINCT portal_item_id) FROM aggregated_by_portal_item");

        Assert.True(serviceCount >= 100, $"Expected a realistic spread of distinct ArcGIS services, found {serviceCount}.");
        Assert.True(portalItemCount >= 100, $"Expected a realistic spread of distinct Portal Items, found {portalItemCount}.");
    }

    private HashSet<string> QueryDistinctRoots()
    {
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);

        return connection.Query<string>("SELECT DISTINCT root FROM aggregated_by_root").ToHashSet(StringComparer.Ordinal);
    }
}
