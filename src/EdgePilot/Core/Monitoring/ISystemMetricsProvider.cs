namespace EdgePilot.Core.Monitoring;

public interface ISystemMetricsProvider
{
    SystemSnapshot Capture();
}
