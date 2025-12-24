using Xunit;

namespace NetCorePal.Aspire.Hosting.DMDB.Tests;

/// <summary>
/// Marks a test that requires a working local Docker daemon.
/// Automatically skips when Docker is unavailable.
/// </summary>
public sealed class RequiresDockerFactAttribute : FactAttribute
{
    public RequiresDockerFactAttribute()
    {
        if (!DockerTestEnvironment.IsDockerAvailable())
        {
            Skip = "Requires Docker (set SKIP_DOCKER_TESTS=1 to force skip).";
            return;
        }

        if (!DockerTestEnvironment.IsAspireOrchestrationAvailable())
        {
            Skip = "Requires Aspire orchestration toolchain (DCP/Dashboard) to be configured.";
        }
    }
}
