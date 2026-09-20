using IisLogParserArcGIS.Reports.Configuration;
using Microsoft.Extensions.Configuration;

namespace IisLogParserArcGIS.Reports.Tests.Configuration;

public class ReportsConfigurationFactoryTests
{
    [Fact]
    public void BindReportsSettings_WithBlankValues_AppliesDocumentedDefaults()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LocalTimeZone"] = string.Empty,
                ["PortalBaseUrl"] = string.Empty,
                ["OutputDirectory"] = string.Empty,
            })
            .Build();

        var settings = ReportsConfigurationFactory.BindReportsSettings(configuration);

        Assert.Equal("UTC", settings.LocalTimeZone);
        Assert.Equal("Dashboard", settings.OutputDirectory);
        Assert.Empty(settings.PortalBaseUrl);
        Assert.Empty(settings.IncludedRoots);
    }

    [Fact]
    public void BindReportsSettings_WithProvidedValues_PreservesThem()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LocalTimeZone"] = "Eastern Standard Time",
                ["PortalBaseUrl"] = "portal.example.com",
                ["OutputDirectory"] = "C:\\CustomDashboard",
                ["IncludedRoots:0"] = "arcgis",
                ["IncludedRoots:1"] = "titan",
            })
            .Build();

        var settings = ReportsConfigurationFactory.BindReportsSettings(configuration);

        Assert.Equal("Eastern Standard Time", settings.LocalTimeZone);
        Assert.Equal("portal.example.com", settings.PortalBaseUrl);
        Assert.Equal("C:\\CustomDashboard", settings.OutputDirectory);
        Assert.Equal(["arcgis", "titan"], settings.IncludedRoots);
    }

    [Fact]
    public void BindReportsSettings_WithNullConfiguration_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ReportsConfigurationFactory.BindReportsSettings(null!));
    }
}
