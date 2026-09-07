namespace EdgePilot.UI;

/// <summary>A continuous damped spring. Retargeting preserves position and velocity.</summary>
internal sealed class NotchSpring
{
    public double Position { get; private set; }
    public double Velocity { get; private set; }
    public double Target { get; set; }
    public bool IsSettled => Math.Abs(Position - Target) < 0.0005 && Math.Abs(Velocity) < 0.005;

    public void Advance(double seconds)
    {
        var remaining = Math.Clamp(seconds, 0, 0.064);
        while (remaining > 0)
        {
            var dt = Math.Min(remaining, 1.0 / 240);
            Velocity += (400 * (Target - Position) - 34 * Velocity) * dt;
            Position += Velocity * dt;
            remaining -= dt;
        }
        if (IsSettled)
        {
            Position = Target;
            Velocity = 0;
        }
    }
}
