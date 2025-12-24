using System.Diagnostics;
using Aspire.Hosting;
using Microsoft.Extensions.Options;

namespace NetCorePal.Aspire.Hosting.DMDB.Tests;

internal static class DockerTestEnvironment
{
    private static readonly Lazy<bool> _isDockerAvailable = new(() =>
    {
        if (!IsEnabledByEnvironmentVariables())
        {
            return false;
        }

        return ProbeDockerCli();
    });

    private static readonly Lazy<bool> _isAspireOrchestrationAvailable = new(() =>
    {
        if (!IsEnabledByEnvironmentVariables())
        {
            return false;
        }

        return ProbeAspireOrchestration();
    });

    internal static bool IsDockerAvailable() => _isDockerAvailable.Value;

    internal static bool IsAspireOrchestrationAvailable() => _isAspireOrchestrationAvailable.Value;

    internal static bool IsContainerIntegrationTestAvailable() => IsDockerAvailable() && IsAspireOrchestrationAvailable();

    internal static bool IsEnabledByEnvironmentVariables()
    {
        return !string.Equals(Environment.GetEnvironmentVariable("SKIP_DOCKER_TESTS"), "1", StringComparison.Ordinal) &&
               !string.Equals(Environment.GetEnvironmentVariable("RUN_DOCKER_TESTS"), "0", StringComparison.Ordinal);
    }

    private static bool ProbeDockerCli()
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "info",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            if (!process.Start())
            {
                return false;
            }

            // Keep this short: discovery-time checks should not hang.
            // 2-second timeout is sufficient for Docker CLI to respond during test discovery
            if (!process.WaitForExit(milliseconds: 2000))
            {
                try { process.Kill(entireProcessTree: true); } catch { /* Ignore cleanup errors */ }
                return false;
            }

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static bool ProbeAspireOrchestration()
    {
        try
        {
            // Validate the Aspire orchestration toolchain (DCP + Dashboard).
            // If these paths are missing, Aspire will throw OptionsValidationException during StartAsync.
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            return Task.Run(async () =>
            {
                var builder = DistributedApplication.CreateBuilder();
                await using var app = builder.Build();
                await app.StartAsync(cts.Token).ConfigureAwait(false);
                await app.StopAsync(cts.Token).ConfigureAwait(false);
                return true;
            }, cts.Token).GetAwaiter().GetResult();
        }
        catch (OptionsValidationException)
        {
            return false;
        }
        catch
        {
            return false;
        }
    }
}
