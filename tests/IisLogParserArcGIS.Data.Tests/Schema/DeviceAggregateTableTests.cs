using IisLogParserArcGIS.Data.Schema;

namespace IisLogParserArcGIS.Data.Tests.Schema;

public class DeviceAggregateTableTests
{
    [Fact]
    public void FieldMaps_NamesTheFieldMapsDeviceTable()
    {
        Assert.Equal(AggregateTableNames.ByFieldMapsDevice, DeviceAggregateTable.FieldMaps.Name);
    }

    [Fact]
    public void Survey123_NamesTheSurvey123DeviceTable()
    {
        Assert.Equal(AggregateTableNames.BySurvey123Device, DeviceAggregateTable.Survey123.Name);
    }

    [Fact]
    public void EveryInstance_NamesARealAggregateTable_AndTheTwoAreDistinct()
    {
        Assert.Contains(DeviceAggregateTable.FieldMaps.Name, AggregateTableNames.All);
        Assert.Contains(DeviceAggregateTable.Survey123.Name, AggregateTableNames.All);
        Assert.NotEqual(DeviceAggregateTable.FieldMaps.Name, DeviceAggregateTable.Survey123.Name);
    }

    [Fact]
    public void NoPublicConstructorExists_SoNoCallerCanBuildOneForAnArbitraryString()
    {
        Assert.Empty(typeof(DeviceAggregateTable).GetConstructors());
    }
}
