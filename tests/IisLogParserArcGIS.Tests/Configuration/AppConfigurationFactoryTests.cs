using IisLogParserArcGIS.Configuration;
using IisLogParserArcGIS.Reports.Configuration;
using Microsoft.Extensions.Configuration;

namespace IisLogParserArcGIS.Tests.Configuration;

public class AppConfigurationFactoryTests
{
    [Fact]
    public void BindAppSettings_WithBlankValues_AppliesDocumentedDefaults()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LocalTimeZone"] = string.Empty,
                ["LogOutputDirectory"] = string.Empty,
                ["LogLevel"] = string.Empty,
                ["PortalWebAdaptorName"] = string.Empty,
            })
            .Build();

        var settings = AppConfigurationFactory.BindAppSettings(configuration);

        Assert.Equal("UTC", settings.LocalTimeZone);
        Assert.Equal("Logs", settings.LogOutputDirectory);
        Assert.Equal("Warning", settings.LogLevel);
        Assert.Equal("portal", settings.PortalWebAdaptorName);
    }

    [Fact]
    public void BindAppSettings_WithProvidedValues_PreservesThem()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LocalTimeZone"] = "Eastern Standard Time",
                ["LogOutputDirectory"] = "C:\\CustomLogs",
                ["LogLevel"] = "Debug",
                ["PortalWebAdaptorName"] = "gis",
            })
            .Build();

        var settings = AppConfigurationFactory.BindAppSettings(configuration);

        Assert.Equal("Eastern Standard Time", settings.LocalTimeZone);
        Assert.Equal("C:\\CustomLogs", settings.LogOutputDirectory);
        Assert.Equal("Debug", settings.LogLevel);
        Assert.Equal("gis", settings.PortalWebAdaptorName);
    }

    [Fact]
    public void BindAppSettings_WithNullConfiguration_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => AppConfigurationFactory.BindAppSettings(null!));
    }

    [Fact]
    public void Build_LetsAPrefixedEnvironmentVariableOverrideTheJsonValue()
    {
        var directory = Directory.CreateTempSubdirectory("iislogparser-config-tests-");
        try
        {
            File.WriteAllText(Path.Combine(directory.FullName, "appsettings.json"), "{ \"LocalTimeZone\": \"America/Edmonton\" }");
            Environment.SetEnvironmentVariable("IISLOGPARSER_LocalTimeZone", "UTC");

            var configuration = AppConfigurationFactory.Build(directory.FullName, "Production");

            Assert.Equal("UTC", configuration["LocalTimeZone"]);
        }
        finally
        {
            Environment.SetEnvironmentVariable("IISLOGPARSER_LocalTimeZone", null);
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public void Build_IgnoresAnUnprefixedEnvironmentVariableOfTheSameName()
    {
        var directory = Directory.CreateTempSubdirectory("iislogparser-config-tests-");
        try
        {
            File.WriteAllText(Path.Combine(directory.FullName, "appsettings.json"), "{ \"LocalTimeZone\": \"America/Edmonton\" }");
            Environment.SetEnvironmentVariable("LocalTimeZone", "UTC");

            var configuration = AppConfigurationFactory.Build(directory.FullName, "Production");

            Assert.Equal("America/Edmonton", configuration["LocalTimeZone"]);
        }
        finally
        {
            Environment.SetEnvironmentVariable("LocalTimeZone", null);
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public void Build_AppliesDevelopmentOverlay_WhenEnvironmentFileExists()
    {
        var directory = Directory.CreateTempSubdirectory("iislogparser-config-tests-");
        try
        {
            File.WriteAllText(Path.Combine(directory.FullName, "appsettings.json"), "{ \"LogLevel\": \"Warning\" }");
            File.WriteAllText(Path.Combine(directory.FullName, "appsettings.Development.json"), "{ \"LogLevel\": \"Debug\" }");

            var configuration = AppConfigurationFactory.Build(directory.FullName, "Development");

            Assert.Equal("Debug", configuration["LogLevel"]);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public void Build_UsesBaseFileOnly_WhenNoOverlayFileExists()
    {
        var directory = Directory.CreateTempSubdirectory("iislogparser-config-tests-");
        try
        {
            File.WriteAllText(Path.Combine(directory.FullName, "appsettings.json"), "{ \"LogLevel\": \"Warning\" }");

            var configuration = AppConfigurationFactory.Build(directory.FullName, "Production");

            Assert.Equal("Warning", configuration["LogLevel"]);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public void Build_ProducesTheOneSharedConfiguration_ThatAlsoBindsReportsSettings()
    {
        var directory = Directory.CreateTempSubdirectory("iislogparser-config-tests-");
        try
        {
            File.WriteAllText(
                Path.Combine(directory.FullName, "appsettings.json"),
                "{ \"LocalTimeZone\": \"America/Edmonton\", \"PortalWebAdaptorName\": \"gis\", \"PortalBaseUrl\": \"portal.example.com\" }");

            var configuration = AppConfigurationFactory.Build(directory.FullName, "Production");
            var appSettings = AppConfigurationFactory.BindAppSettings(configuration);
            var reportsSettings = ReportsConfigurationFactory.BindReportsSettings(configuration);

            Assert.Equal("America/Edmonton", appSettings.LocalTimeZone);
            Assert.Equal("gis", appSettings.PortalWebAdaptorName);
            Assert.Equal("America/Edmonton", reportsSettings.LocalTimeZone);
            Assert.Equal("portal.example.com", reportsSettings.PortalBaseUrl);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }
}
