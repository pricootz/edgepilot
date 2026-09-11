namespace EdgePilot.Core.Signals;

public sealed record Signal
{
    public string Id { get; }
    public SignalSource Source { get; }
    public SignalSeverity Severity { get; }
    public string TitleKey { get; }
    public string MessageKey { get; }
    public DateTimeOffset CreatedAt { get; }
    public TimeSpan TimeToLive { get; }
    public string DedupeKey { get; }

    public DateTimeOffset ExpiresAt => CreatedAt + TimeToLive;

    public Signal(
        string id,
        SignalSource source,
        SignalSeverity severity,
        string titleKey,
        string messageKey,
        DateTimeOffset createdAt,
        TimeSpan timeToLive,
        string? dedupeKey = null)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Signal id is required.", nameof(id));
        if (string.IsNullOrWhiteSpace(titleKey)) throw new ArgumentException("Signal title key is required.", nameof(titleKey));
        if (string.IsNullOrWhiteSpace(messageKey)) throw new ArgumentException("Signal message key is required.", nameof(messageKey));
        if (timeToLive <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeToLive));

        Id = id;
        Source = source;
        Severity = severity;
        TitleKey = titleKey;
        MessageKey = messageKey;
        CreatedAt = createdAt;
        TimeToLive = timeToLive;
        DedupeKey = string.IsNullOrWhiteSpace(dedupeKey) ? id : dedupeKey;
    }
}
