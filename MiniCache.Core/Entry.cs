namespace MiniCache.Core;

internal sealed record Entry(ReadOnlyMemory<byte> Value, DateTimeOffset CreatedAt, DateTimeOffset? ExpiresAt, long Hits = 0)
{
    public long Hits { get; private set; } = Hits;
    public void IncreaseHits() => Hits++;
}