namespace MiniCache.Core;

/// <summary>
/// Represents metadata for a cache entry, including creation time, optional expiration, and access count.
/// </summary>
/// <param name="createdAt">The date and time when the cache entry was created.</param>
/// <param name="expiresAt">The date and time when the cache entry expires, or <see langword="null"/> if the entry does not expire.</param>
/// <param name="hits">The number of times the cache entry has been accessed.</param>
public readonly struct CacheEntryMetadata(DateTimeOffset createdAt, DateTimeOffset? expiresAt, long hits)
{
    public DateTimeOffset CreatedAt { get; } = createdAt;
    public DateTimeOffset? ExpiresAt { get; } = expiresAt;
    public long Hits { get; } = hits;
}
