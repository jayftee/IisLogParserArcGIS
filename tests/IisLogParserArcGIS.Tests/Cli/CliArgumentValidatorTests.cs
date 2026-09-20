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
    public void Validate_WithWellFormedHarvestRegenerateArguments_ReturnsParsedArguments()
    {
        var arguments = new HarvestRegenerateArguments
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
            var arguments = new RegenerateArguments { InputDatabasePath = tempPath };

            var parsed = CliArgumentValidator.Validate(arguments);

            Assert.Equal(Path.GetFullPath(tempPath), parsed.InputDatabasePath);
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
        var arguments = new RegenerateArguments { InputDatabasePath = nonExistentPath };

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
        var arguments = new RegenerateArguments { InputDatabasePath = blank };

        var ex = Assert.Throws<CliArgumentValidationException>(() => CliArgumentValidator.Validate(arguments));
        Assert.Contains("input database path", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_WithInvalidRegenerateArgumentsPathCharacters_ThrowsCliArgumentValidationExceptionWithInnerException()
    {
        var arguments = new RegenerateArguments { InputDatabasePath = "out\0withnull.db" };

        var ex = Assert.Throws<CliArgumentValidationException>(() => CliArgumentValidator.Validate(arguments));
        Assert.NotNull(ex.InnerException);
    }
}
