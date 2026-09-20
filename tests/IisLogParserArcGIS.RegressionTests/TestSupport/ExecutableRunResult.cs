namespace IisLogParserArcGIS.RegressionTests.TestSupport;

/// <summary>
/// The external, observable outcome of running the compiled console executable as a black-box process.
/// </summary>
/// <param name="ExitCode">The process's exit code.</param>
/// <param name="StandardOutput">Everything the process wrote to stdout.</param>
/// <param name="StandardError">Everything the process wrote to stderr.</param>
internal sealed record ExecutableRunResult(int ExitCode, string StandardOutput, string StandardError);
