namespace Quality.Cli.Process;

using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using SystemProcess = System.Diagnostics.Process;

[ExcludeFromCodeCoverage(Justification = "Out-of-process invocation helper; exercised by the checks/commands that wrap it (LicenseCheck, LockfileIntegrityCheck, EfMigrationsDriftCheck, PrCheckCommand, DoctorCommand).")]
internal static class ProcessRunner
{
    public static (int ExitCode, string Stdout, string Stderr, bool TimedOut) Run(
        string exe,
        string args,
        string? workingDir,
        TimeSpan timeout,
        bool captureOutput = true)
    {
        var psi = BuildStartInfo(exe, args, workingDir, captureOutput);
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        using var proc = new SystemProcess { StartInfo = psi };

        if (captureOutput)
        {
            AttachReaders(proc, stdout, stderr);
        }

        proc.Start();

        if (captureOutput)
        {
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
        }

        if (!proc.WaitForExit((int)timeout.TotalMilliseconds))
        {
            TryKill(proc);
            return (-1, stdout.ToString(), stderr.ToString(), true);
        }

        return (proc.ExitCode, stdout.ToString(), stderr.ToString(), false);
    }

    private static ProcessStartInfo BuildStartInfo(string exe, string args, string? workingDir, bool captureOutput)
    {
        var psi = new ProcessStartInfo(exe, args)
        {
            UseShellExecute = false,
            RedirectStandardOutput = captureOutput,
            RedirectStandardError = captureOutput,
        };
        if (workingDir is not null)
        {
            psi.WorkingDirectory = workingDir;
        }

        // Without these env vars, child MSBuild worker nodes inherit the parent's
        // stdout/stderr handles and keep them open past process exit, deadlocking
        // WaitForExit() on stream drain. Applied unconditionally — harmless for
        // non-dotnet invocations.
        psi.Environment["MSBUILDDISABLENODEREUSE"] = "1";
        psi.Environment["DOTNET_CLI_USE_MSBUILD_SERVER"] = "0";
        return psi;
    }

    private static void AttachReaders(SystemProcess proc, StringBuilder stdout, StringBuilder stderr)
    {
        proc.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                lock (stdout)
                {
                    stdout.AppendLine(e.Data);
                }
            }
        };
        proc.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                lock (stderr)
                {
                    stderr.AppendLine(e.Data);
                }
            }
        };
    }

    private static void TryKill(SystemProcess proc)
    {
        try
        {
            proc.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // Process already exited between the timeout check and the kill — nothing to do.
        }
        catch (Win32Exception)
        {
            // Insufficient privileges or transient OS error — best-effort cleanup, swallow.
        }
    }
}
