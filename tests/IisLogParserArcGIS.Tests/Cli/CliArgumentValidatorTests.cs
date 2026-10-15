using IisLogParserArcGIS.Cli;

namespace IisLogParserArcGIS.Tests.Cli;

public class CliArgumentValidatorTests
{
    [Fact]
    public void Validate_WithWellFormedHarvestArguments_ReturnsParsedArguments()
    {
        var arguments = new HarvestArguments
        {
            LogSourceDirectory = "logs",
            TargetLocalDate = "2026-05-01",
            OutputDatabasePath = "out.db",
        };

        var parsed = CliArgumentValidator.Validate(arguments);

        Assert.Equal(new DateOnly(2026, 5, 1), parsed.TargetLocalDate);
        Assert.EndsWith("logs", parsed.LogSourceDirectory, StringComparison.Ordinal);
        Assert.EndsWith("out.db", parsed.OutputDatabasePath, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_WithWellFormedHarvestRegenerateArguments_ReturnsParsedArgumentsWithTheFullOutputDirectory()
    {
        var arguments = new HarvestRegenerateArguments
        {
            LogSourceDirectory = "logs",
            TargetLocalDate = "2026-05-01",
            OutputDatabasePath = "out.db",
            OutputDirectory = "dashboard",
        };

        var parsed = CliArgumentValidator.Validate(arguments);

        Assert.Equal(new DateOnly(2026, 5, 1), parsed.Harvest.TargetLocalDate);
        Assert.EndsWith("logs", parsed.Harvest.LogSourceDirectory, StringComparison.Ordinal);
        Assert.EndsWith("out.db", parsed.Harvest.OutputDatabasePath, StringComparison.Ordinal);
        Assert.Equal(Path.GetFullPath("dashboard"), parsed.OutputDirectory);
    }

    [Fact]
    public void Validate_WithHarvestRegenerateArguments_TakesTheYearFromTheTargetLocalDate()
    {
        var arguments = new HarvestRegenerateArguments
        {
            LogSourceDirectory = "logs",
            TargetLocalDate = "2025-12-31",
            OutputDatabasePath = "out.db",
            OutputDirectory = "dashboard",
        };

        var parsed = CliArgumentValidator.Validate(arguments);

        Assert.Equal(2025, parsed.Year);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithBlankHarvestRegenerateOutputDirectory_ThrowsCliArgumentValidationException(string blank)
    {
        var arguments = new HarvestRegenerateArguments
        {
            LogSourceDirectory = "logs",
            TargetLocalDate = "2026-05-01",
            OutputDatabasePath = "out.db",
            OutputDirectory = blank,
        };

        var ex = Assert.Throws<CliArgumentValidationException>(() => CliArgumentValidator.Validate(arguments));
        Assert.Contains("output directory", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_WithNullHarvestRegenerateArguments_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => CliArgumentValidator.Validate((HarvestRegenerateArguments)null!));
    }

    [Fact]
    public void Validate_WithNullHarvestArguments_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => CliArgumentValidator.Validate((IHarvestArguments)null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithBlankLogSourceDirectory_ThrowsCliArgumentValidationException(string blank)
    {
        var arguments = new HarvestArguments
        {
            LogSourceDirectory = blank,
            TargetLocalDate = "2026-05-01",
            OutputDatabasePath = "out.db",
        };

        var ex = Assert.Throws<CliArgumentValidationException>(() => CliArgumentValidator.Validate(arguments));
        Assert.Contains("log source directory", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_WithBlankOutputDatabasePath_ThrowsCliArgumentValidationException()
    {
        var arguments = new HarvestArguments
        {
            LogSourceDirectory = "logs",
            TargetLocalDate = "2026-05-01",
            OutputDatabasePath = string.Empty,
        };

        var ex = Assert.Throws<CliArgumentValidationException>(() => CliArgumentValidator.Validate(arguments));
        Assert.Contains("output database path", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("not-a-date")]
    [InlineData("2026/05/01")]
    [InlineData("05-01-2026")]
    public void Validate_WithMalformedDate_ThrowsCliArgumentValidationException(string malformedDate)
    {
        var arguments = new HarvestArguments
        {
            LogSourceDirectory = "logs",
            TargetLocalDate = malformedDate,
            OutputDatabasePath = "out.db",
        };

        var ex = Assert.Throws<CliArgumentValidationException>(() => CliArgumentValidator.Validate(arguments));
        Assert.Contains(malformedDate, ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_WithInvalidPathCharacters_ThrowsCliArgumentValidationExceptionWithInnerException()
    {
        var arguments = new HarvestArguments
        {
            LogSourceDirectory = "logs\0withnull",
            TargetLocalDate = "2026-05-01",
            OutputDatabasePath = "out.db",
        };

        var ex = Assert.Throws<CliArgumentValidationException>(() => CliArgumentValidator.Validate(arguments));
        Assert.NotNull(ex.InnerException);
    }

    [Fact]
    public void Validate_WithWellFormedRegenerateArguments_ReturnsParsedRegenerateArguments()
    {
        var tempPath = Path.GetTempFileName();
        try
        {
            var arguments = new RegenerateArguments { OutputDirectory = "dashboard", Year = 2026, InputDatabasePath = tempPath };

            var parsed = CliArgumentValidator.Validate(arguments);

            Assert.Equal(Path.GetFullPath(tempPath), parsed.InputDatabasePath);
            Assert.Equal(Path.GetFullPath("dashboard"), parsed.OutputDirectory);
            Assert.Equal(2026, parsed.Year);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public void Validate_WithNonExistentInputDatabasePath_ThrowsCliArgumentValidationException()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.sqlite");
        var arguments = new RegenerateArguments { OutputDirectory = "dashboard", Year = 2026, InputDatabasePath = nonExistentPath };

        var ex = Assert.Throws<CliArgumentValidationException>(() => CliArgumentValidator.Validate(arguments));
        Assert.Contains("does not exist", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_WithNullRegenerateArguments_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => CliArgumentValidator.Validate((RegenerateArguments)null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithBlankInputDatabasePath_ThrowsCliArgumentValidationException(string blank)
    {
        var arguments = new RegenerateArguments { OutputDirectory = "dashboard", Year = 2026, InputDatabasePath = blank };

        var ex = Assert.Throws<CliArgumentValidationException>(() => CliArgumentValidator.Validate(arguments));
        Assert.Contains("input database path", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_WithInvalidRegenerateArgumentsPathCharacters_ThrowsCliArgumentValidationExceptionWithInnerException()
    {
        var arguments = new RegenerateArguments { OutputDirectory = "dashboard", Year = 2026, InputDatabasePath = "out\0withnull.db" };

        var ex = Assert.Throws<CliArgumentValidationException>(() => CliArgumentValidator.Validate(arguments));
        Assert.NotNull(ex.InnerException);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithBlankRegenerateOutputDirectory_ThrowsCliArgumentValidationException(string blank)
    {
        var arguments = new RegenerateArguments { InputDatabasePath = "in.db", OutputDirectory = blank, Year = 2026 };

        var ex = Assert.Throws<CliArgumentValidationException>(() => CliArgumentValidator.Validate(arguments));
        Assert.Contains("output directory", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2026)]
    [InlineData(26)]
    [InlineData(999)]
    [InlineData(10000)]
    public void Validate_WithAYearThatIsNotFourDigits_ThrowsCliArgumentValidationException(int year)
    {
        var tempPath = Path.GetTempFileName();
        try
        {
            var arguments = new RegenerateArguments { InputDatabasePath = tempPath, OutputDirectory = "dashboard", Year = year };

            var ex = Assert.Throws<CliArgumentValidationException>(() => CliArgumentValidator.Validate(arguments));
            Assert.Contains(year.ToString(System.Globalization.CultureInfo.InvariantCulture), ex.Message, StringComparison.Ordinal);
            Assert.Contains("year", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }
}
