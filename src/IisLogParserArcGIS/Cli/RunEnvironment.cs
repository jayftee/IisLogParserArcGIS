namespace IisLogParserArcGIS.Cli;

/// <summary>
/// Everything a verb's runner needs from the process it runs in, injected instead of read from statics so the
/// runners can be driven against a temporary directory with a captured error stream.
/// </summary>
/// <param name="BaseDirectory">
/// The executable's own directory: where <c>appsettings.json</c> is read from and a relative log or Dashboard
/// output directory is resolved against.
/// </param>
/// <param name="EnvironmentName">
/// The hosting environment name (<c>DOTNET_ENVIRONMENT</c>, or <c>Production</c> when unset) selecting the
/// <c>appsettings.{EnvironmentName}.json</c> overlay.
/// </param>
/// <param name="Error">The writer operator-facing failure messages go to (the console's error stream).</param>
/// <param name="TimeProvider">The clock a Regeneration Run resolves "today" and "this year" from.</param>
#pragma warning disable CC0042 // Four independent process-level inputs bundled into one parameter object; splitting them would only push the same four values through every runner.
public sealed record RunEnvironment(string BaseDirectory, string EnvironmentName, TextWriter Error, TimeProvider TimeProvider);
#pragma warning restore CC0042
