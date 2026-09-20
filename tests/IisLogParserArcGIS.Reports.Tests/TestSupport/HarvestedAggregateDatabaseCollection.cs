namespace IisLogParserArcGIS.Reports.Tests.TestSupport;

/// <summary>
/// Groups every test that consumes <see cref="HarvestedAggregateDatabaseFixture"/>, so xUnit builds that
/// expensive fixture exactly once per test run instead of once per test class. Apply
/// <c>[Collection(HarvestedAggregateDatabaseCollection.Name)]</c> to a test class and take
/// <see cref="HarvestedAggregateDatabaseFixture"/> as a constructor parameter to use it.
/// </summary>
#pragma warning disable CA1711 // "Collection" is xUnit's own naming convention for a collection definition class, not a .NET collection type.
[CollectionDefinition(Name)]
public sealed class HarvestedAggregateDatabaseCollection : ICollectionFixture<HarvestedAggregateDatabaseFixture>
#pragma warning restore CA1711
{
    /// <summary>
    /// The xUnit collection name tests reference via <see cref="CollectionAttribute"/>.
    /// </summary>
    public const string Name = "Harvested aggregate database";
}
