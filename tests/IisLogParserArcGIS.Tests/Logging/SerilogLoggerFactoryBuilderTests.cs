using IisLogParserArcGIS.Configuration;
using IisLogParserArcGIS.Logging;
using Microsoft.Extensions.Logging;

namespace IisLogParserArcGIS.Tests.Logging;

public class SerilogLoggerFactoryBuilderTests
{
    [Fact]
    public void Create_WritesToRollingFile_UnderConfiguredDirectory()
    {
        var baseDirectory = Directory.CreateTempSubdirectory("iislogparser-logging-tests-");
        try
        {
            var settings = new AppSettings
            {
                LocalTimeZone = "UTC",
                LogOutputDirectory = "Logs",
                LogLevel = "Warning",
            };

            using (var loggerFactory = SerilogLoggerFactoryBuilder.Create(settings, baseDirectory.FullName))
            {
                var logger = loggerFactory.CreateLogger("SerilogLoggerFactoryBuilderTests");
                logger.LogWarning("test message {Marker}", "abc123");
            }

            var logDirectory = Path.Combine(baseDirectory.FullName, "Logs");
            Assert.True(Directory.Exists(logDirectory));

            var logFile = Directory.GetFiles(logDirectory, "iislogparser*.log").Single();
            var contents = File.ReadAllText(logFile);
            Assert.Contains("test message", contents, StringComparison.Ordinal);
            Assert.Contains("abc123", contents, StringComparison.Ordinal);
        }
        finally
        {
            baseDirectory.Delete(recursive: true);
        }
    }

    [Fact]
    public void Create_WithNullSettings_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => SerilogLoggerFactoryBuilder.Create(null!, Path.GetTempPath()));
    }
}
