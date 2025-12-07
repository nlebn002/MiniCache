namespace MiniCache.Core;

/// <summary>
/// Defines the contract for a key-value cache that stores binary data and associated metadata, supporting retrieval,
/// insertion, removal, and enumeration of cached entries.
/// </summary>
/// <remarks>Implementations of this interface provide mechanisms for managing cached data, including optional
/// time-to-live (TTL) expiration and metadata retrieval. Thread safety and eviction policies may vary depending on the
/// implementation. Keys are typically case-sensitive and must be unique within the cache.</remarks>
public interface ICache
{
    bool TryGet(string key, out ReadOnlyMemory<byte> value);

    void Set(string key, ReadOnlyMemory<byte> value, TimeSpan? ttl = null);

    bool Remove(string key);

    void Clear();

    long Count { get; }

    bool TryGetMetadata(string key, out CacheEntryMetadata metadata);
}