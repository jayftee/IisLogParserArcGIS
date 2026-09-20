using System.Diagnostics;
using System.Threading;

namespace IisLogParserArcGIS.RegressionTests.TestSupport;

/// <summary>
/// Runs an external executable as a black-box process, capturing its exit code and redirected output streams.
/// </summary>
internal static class ProcessRunner
{
    // A named binary Semaphore, not a Mutex - RunAsync spans several awaits, and a Mutex must be released on the
    // exact thread that acquired it, which an async method's continuation can't guarantee once it resumes on a
    // different thread-pool thread. The shipped IisLogParserArcGIS.exe, as a side effect of every run, writes to
    // its own rolling log file under LogOutputDirectory, next to itself (see SerilogLoggerFactoryBuilder) - a
    // shared, non-test-isolated location. This project's own test suite and IisLogParserArcGIS.Reports.Tests's
    // harvested-database fixture both invoke the executable through this method, in separate test-host
    // processes, so every invocation on the machine is serialized against every other one to avoid racing that
    // shared log file.
    // The shipped appsettings.json is this deployment's real configuration (America/Edmonton), but every oracle in
    // the regression and Reports suites assumes a request's local date is its raw UTC date, so every run they make
    // pins the time zone to UTC, for that one process only, via the executable's IISLOGPARSER_ override prefix.
    private const string UtcTimeZoneOverrideVariable = "IISLOGPARSER_LocalTimeZone";

    private static readonly Semaphore _sharedExecutableSemaphore = new(1, 1, @"Local\IisLogParserArcGIS.CompiledProgram.ProcessRunner");

    /// <summary>
    /// Starts <paramref name="executablePath"/> with <paramref name="arguments"/>, waits for it to exit, and
    /// returns its exit code and captured stdout/stderr. Serialized against every other concurrent call to this
    /// method, on this machine, across processes.
    /// </summary>
    /// <param name="executablePath">The full path of the executable to run.</param>
    /// <param name="arguments">The command-line arguments to pass, in order.</param>
    /// <param name="timeout">How long to wait for the process to exit before killing it.</param>
    /// <returns>The process's external, observable outcome.</returns>
    /// <exception cref="TimeoutException">The process did not exit within <paramref name="timeout"/>.</exception>
    public static async Task<ExecutableRunResult> RunAsync(string executablePath, IReadOnlyList<string> arguments, TimeSpan timeout)
    {
        ArgumentNullException.ThrowIfNull(executablePath);
        ArgumentNullException.ThrowIfNull(arguments);

        if (_sharedExecutableSemaphore.WaitOne(TimeSpan.FromMinutes(5)) is false)
        {
            throw new TimeoutException(
                $"Timed out waiting for another process to finish using the shared '{Path.GetFileName(executablePath)}'.");
        }

        try
        {
            var startInfo = new ProcessStartInfo(executablePath)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(executablePath),
            };

            startInfo.Environment[UtcTimeZoneOverrideVariable] = "UTC";

            foreach (var argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var standardOutputTask = process.StandardOutput.ReadToEndAsync();
            var standardErrorTask = process.StandardError.ReadToEndAsync();

            using var timeoutCancellation = new CancellationTokenSource(timeout);

            try
            {
                await process.WaitForExitAsync(timeoutCancellation.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                process.Kill(entireProcessTree: true);
                throw new TimeoutException($"'{executablePath}' did not exit within {timeout}.");
            }

            return new ExecutableRunResult(
                process.ExitCode,
                await standardOutputTask.ConfigureAwait(false),
                await standardErrorTask.ConfigureAwait(false));
        }
        finally
        {
            _sharedExecutableSemaphore.Release();
        }
    }
}
