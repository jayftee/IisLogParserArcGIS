using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace IisLogParserArcGIS.Tests.Scripts;

/// <summary>
/// Verifies the shipped PowerShell scripts and the <c>.bat</c> wrapper templates a Scheduled Task launches
/// (ticket 30): every template passes every mandatory parameter of the script it calls (a Scheduled Task that
/// omits one fails at 02:00 with nobody watching), and the daily script refuses a missing or unparseable date
/// before it ever looks for the executable.
/// </summary>
public static partial class ScriptTemplateTests
{
    public static TheoryData<string, string> TemplatesAndTheirScripts => new()
    {
        { "IisLogBackfill.bat", "Invoke-IisLogBackfill.ps1" },
        { "IisLogHourly.bat", "Invoke-IisLogDailyRun.ps1" },
        { "IisLogNightly.bat", "Invoke-IisLogDailyRun.ps1" },
    };

    [Theory]
    [MemberData(nameof(TemplatesAndTheirScripts))]
    public static void Template_CallsTheExpectedScript(string template, string script)
    {
        var templateText = File.ReadAllText(ScriptPath(template));

        Assert.Contains($"\"%~dp0{script}\"", templateText, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(TemplatesAndTheirScripts))]
    public static void Template_PassesEveryMandatoryParameterOfTheScriptItCalls(string template, string script)
    {
        var passed = PassedParameterNames(File.ReadAllText(ScriptPath(template)));
        var mandatory = MandatoryParameterNames(File.ReadAllText(ScriptPath(script)));

        Assert.NotEmpty(mandatory);
        Assert.Empty(mandatory.Except(passed, StringComparer.OrdinalIgnoreCase));
    }

    [Theory]
    [MemberData(nameof(TemplatesAndTheirScripts))]
    public static void Template_PassesNoParameterTheScriptDoesNotDeclare(string template, string script)
    {
        var passed = PassedParameterNames(File.ReadAllText(ScriptPath(template)));
        var declared = DeclaredParameterNames(File.ReadAllText(ScriptPath(script)));

        Assert.Empty(passed.Except(declared, StringComparer.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("IisLogBackfill.bat")]
    [InlineData("IisLogHourly.bat")]
    [InlineData("IisLogNightly.bat")]
    public static void Template_UsesWindowsLineEndings(string template)
    {
        var bytes = File.ReadAllBytes(ScriptPath(template));
        var text = Encoding.ASCII.GetString(bytes);

        Assert.DoesNotContain("\n", text.Replace("\r\n", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("IisLogHourly.bat")]
    [InlineData("IisLogNightly.bat")]
    public static void DailyTemplate_TakesTheDatabaseAndDashboardYearFromTheTargetDate_NotTheClock(string template)
    {
        var templateText = File.ReadAllText(ScriptPath(template));

        Assert.Contains("%TARGET_DATE:~0,4%", templateText, StringComparison.Ordinal);
        Assert.Contains("if not defined TARGET_DATE", templateText, StringComparison.Ordinal);
        Assert.Contains("-TargetDate %TARGET_DATE%", templateText, StringComparison.Ordinal);
        Assert.DoesNotContain("-Yesterday", templateText, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("not-a-date")]
    [InlineData("2026/10/14")]
    [InlineData("14-10-2026")]
    [InlineData("2026-13-01")]
    public static async Task DailyRun_WithAnUnparseableTargetDate_FailsBeforeLookingForTheExecutable(string targetDate)
    {
        var (exitCode, error) = await RunScriptAsync(
            "Invoke-IisLogDailyRun.ps1", "-LogSourceDirectory", "logs", "-OutputDatabasePath", "x.sqlite", "-OutputDirectory", "dash", "-TargetDate", targetDate);

        Assert.NotEqual(0, exitCode);
        Assert.Contains("TargetDate", error, StringComparison.Ordinal);
        Assert.DoesNotContain("Could not find IisLogParserArcGIS.exe", error, StringComparison.Ordinal);
    }

    [Fact]
    public static async Task DailyRun_WithoutATargetDate_Fails()
    {
        var (exitCode, error) = await RunScriptAsync(
            "Invoke-IisLogDailyRun.ps1", "-LogSourceDirectory", "logs", "-OutputDatabasePath", "x.sqlite", "-OutputDirectory", "dash");

        Assert.NotEqual(0, exitCode);
        Assert.Contains("TargetDate", error, StringComparison.Ordinal);
    }

    [Fact]
    public static async Task DailyRun_NoLongerAcceptsTheYesterdaySwitch()
    {
        var (exitCode, error) = await RunScriptAsync(
            "Invoke-IisLogDailyRun.ps1", "-LogSourceDirectory", "logs", "-OutputDatabasePath", "x.sqlite", "-OutputDirectory", "dash", "-TargetDate", "2026-10-14", "-Yesterday");

        Assert.NotEqual(0, exitCode);
        Assert.Contains("Yesterday", error, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("26")]
    [InlineData("10000")]
    public static async Task Backfill_WithAYearThatIsNotFourDigits_Fails(string year)
    {
        var (exitCode, error) = await RunScriptAsync(
            "Invoke-IisLogBackfill.ps1", "-LogSourceDirectory", "logs", "-OutputDatabasePath", "x.sqlite", "-OutputDirectory", "dash", "-Year", year);

        Assert.NotEqual(0, exitCode);
        Assert.Contains("Year", error, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("-LogSourceDirectory")]
    [InlineData("-OutputDatabasePath")]
    [InlineData("-OutputDirectory")]
    [InlineData("-Year")]
    public static async Task Backfill_WithoutAMandatoryParameter_Fails(string omitted)
    {
        string[] allArguments = ["-LogSourceDirectory", "logs", "-OutputDatabasePath", "x.sqlite", "-OutputDirectory", "dash", "-Year", "2026"];
        var index = Array.IndexOf(allArguments, omitted);
        var arguments = allArguments.Where((_, position) => position != index && position != index + 1).ToArray();

        var (exitCode, _) = await RunScriptAsync("Invoke-IisLogBackfill.ps1", arguments);

        Assert.NotEqual(0, exitCode);
    }

    private static HashSet<string> PassedParameterNames(string templateText)
    {
        var start = templateText.IndexOf("powershell.exe", StringComparison.Ordinal);
        Assert.True(start >= 0, "The template has no powershell.exe invocation.");
        var invocation = templateText[start..];
        var end = invocation.IndexOf("exit /b", StringComparison.Ordinal);
        invocation = end >= 0 ? invocation[..end] : invocation;

        return ParameterTokenPattern().Matches(invocation).Select(match => match.Groups[1].Value)
            .Except(["NoProfile", "ExecutionPolicy", "File"], StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static HashSet<string> MandatoryParameterNames(string scriptText)
    {
        return ParameterBlocks(scriptText).Where(block => block.IsMandatory).Select(block => block.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static HashSet<string> DeclaredParameterNames(string scriptText)
    {
        return ParameterBlocks(scriptText).Select(block => block.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<(bool IsMandatory, string Name)> ParameterBlocks(string scriptText)
    {
        var paramStart = scriptText.IndexOf("param(", StringComparison.OrdinalIgnoreCase);
        Assert.True(paramStart >= 0, "The script has no param() block.");
        var paramBlock = scriptText[paramStart..];

        foreach (var chunk in paramBlock.Split("[Parameter(", StringSplitOptions.RemoveEmptyEntries).Skip(1))
        {
            var names = TypedVariablePattern().Matches(chunk);
            Assert.NotEmpty(names);
            yield return (chunk.Contains("Mandatory = $true", StringComparison.Ordinal), names[^1].Groups[1].Value);
        }
    }

    private static async Task<(int ExitCode, string Error)> RunScriptAsync(string script, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo("powershell.exe")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-NonInteractive");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(ScriptPath(script));

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)!;
        process.StandardInput.Close();
        var errorTask = process.StandardError.ReadToEndAsync();
        var outputTask = process.StandardOutput.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));

        try
        {
            await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw;
        }

        return (process.ExitCode, await errorTask.ConfigureAwait(false) + await outputTask.ConfigureAwait(false));
    }

    private static string ScriptPath(string fileName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && File.Exists(Path.Combine(directory.FullName, "IisLogParserArcGIS.slnx")) is false)
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return Path.Combine(directory.FullName, "src", "IisLogParserArcGIS", "Scripts", fileName);
    }

    [GeneratedRegex(@"(?<![\w-])-([A-Za-z]+)")]
    private static partial Regex ParameterTokenPattern();

    [GeneratedRegex(@"\[(?:string|int|switch)\]\$(\w+)")]
    private static partial Regex TypedVariablePattern();
}
