using EdgePilot.Core;

namespace EdgePilot.Core.Monitoring;

public interface ISystemMetricsProvider
{
    SystemSnapshot Capture();
}
