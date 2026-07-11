namespace BeamKit.Release.Tests;

internal sealed class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset timestamp;

    public FixedTimeProvider(DateTimeOffset timestamp)
    {
        this.timestamp = timestamp;
    }

    public override DateTimeOffset GetUtcNow()
    {
        return timestamp;
    }
}
