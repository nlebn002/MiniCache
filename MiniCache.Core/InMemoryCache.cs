using System;
using System.Collections.Concurrent;
using System.Threading;

namespace MiniCache.Core;

/// <summary>
/// An in-memory implementation that prioritizes throughput by avoiding locks on every operation while still tracking TTL/metadata.
/// </summary>
public sealed class InMemoryCache : ICache, IDisposable
{
    private readonly ConcurrentDictionary<string, Entry> _entries = new();
    private readonly Timer _cleanupTimer;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(1);

    public InMemoryCache()
    {
        _cleanupTimer = new Timer(_ => CleanupExpiredEntries(), null, _cleanupInterval, _cleanupInterval);
    }

    public long Count => _entries.Count;

    public void Clear()
    {
        _entries.Clear();
    }

    public bool Remove(string key)
    {
        return _entries.TryRemove(key, out _);
    }

    public void Set(string key, ReadOnlyMemory<byte> value, TimeSpan? ttl = null)
    {
        var now = DateTimeOffset.UtcNow;
        DateTimeOffset? expiresAt = ttl.HasValue ? now.Add(ttl.Value) : null;
        _entries[key] = new Entry(value, now, expiresAt);
    }

    public bool TryGet(string key, out ReadOnlyMemory<byte> value)
    {
        if (!_entries.TryGetValue(key, out var entry) || entry.IsExpired())
        {
            _entries.TryRemove(key, out _);
            value = default;
            return false;
        }

        entry.IncreaseHits();
        value = entry.Value;
        return true;
    }

    public bool TryGetMetadata(string key, out CacheEntryMetadata metadata)
    {
        metadata = default;
        if (!_entries.TryGetValue(key, out var entry) || entry.IsExpired())
        {
            _entries.TryRemove(key, out _);
            return false;
        }

        metadata = new(entry.CreatedAt, entry.ExpiresAt, entry.Hits);
        return true;
    }

    private void CleanupExpiredEntries()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var (key, entry) in _entries)
        {
            if (entry.IsExpired(now))
            {
                _entries.TryRemove(key, out _);
            }
        }
    }

    public void Dispose()
    {
        _cleanupTimer.Dispose();
    }

    private sealed class Entry
    {
        private long _hits;

        public Entry(ReadOnlyMemory<byte> value, DateTimeOffset createdAt, DateTimeOffset? expiresAt)
        {
            Value = value;
            CreatedAt = createdAt;
            ExpiresAt = expiresAt;
        }

        public ReadOnlyMemory<byte> Value { get; }

        public DateTimeOffset CreatedAt { get; }

        public DateTimeOffset? ExpiresAt { get; }

        public long Hits => Interlocked.Read(ref _hits);

        public void IncreaseHits()
        {
            Interlocked.Increment(ref _hits);
        }

        public bool IsExpired(DateTimeOffset now)
        {
            return ExpiresAt.HasValue && ExpiresAt.Value <= now;
        }

        public bool IsExpired()
        {
            return IsExpired(DateTimeOffset.UtcNow);
        }
    }
}
