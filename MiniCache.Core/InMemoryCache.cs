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
    private readonly ConcurrentBag<Entry> _entryPool = new();
    private readonly Timer _cleanupTimer;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(1);

    public InMemoryCache()
    {
        _cleanupTimer = new Timer(_ => CleanupExpiredEntries(), null, _cleanupInterval, _cleanupInterval);
    }

    public long Count => _entries.Count;

    public void Clear()
    {
        foreach (var key in _entries.Keys)
        {
            if (_entries.TryRemove(key, out var entry))
            {
                ReleaseEntry(entry);
            }
        }
    }

    public bool Remove(string key)
    {
        if (_entries.TryRemove(key, out var entry))
        {
            ReleaseEntry(entry);
            return true;
        }

        return false;
    }

    public void Set(string key, ReadOnlyMemory<byte> value, TimeSpan? ttl = null)
    {
        var now = DateTimeOffset.UtcNow;
        DateTimeOffset? expiresAt = ttl.HasValue ? now.Add(ttl.Value) : null;

        while (true)
        {
            if (_entries.TryGetValue(key, out var existing))
            {
                existing.Reset(value, now, expiresAt);
                return;
            }

            var entry = AcquireEntry(value, now, expiresAt);
            if (_entries.TryAdd(key, entry))
            {
                return;
            }

            if (_entries.TryGetValue(key, out existing))
            {
                existing.Reset(value, now, expiresAt);
                ReleaseEntry(entry);
                return;
            }

            ReleaseEntry(entry);
        }
    }

    public bool TryGet(string key, out ReadOnlyMemory<byte> value)
    {
        if (!_entries.TryGetValue(key, out var entry) || entry.IsExpired())
        {
            if (_entries.TryRemove(key, out var removed))
            {
                ReleaseEntry(removed);
            }
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
            if (_entries.TryRemove(key, out var expiredEntry))
            {
                ReleaseEntry(expiredEntry);
            }
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
                if (_entries.TryRemove(key, out var removed))
                {
                    ReleaseEntry(removed);
                }
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
            _hits = 0;
        }

        public ReadOnlyMemory<byte> Value { get; private set; }

        public DateTimeOffset CreatedAt { get; private set; }

        public DateTimeOffset? ExpiresAt { get; private set; }

        public long Hits => Interlocked.Read(ref _hits);

        public void IncreaseHits()
        {
            Interlocked.Increment(ref _hits);
        }

        public bool IsExpired(DateTimeOffset now)
        {
            return ExpiresAt.HasValue && ExpiresAt.Value <= now;
        }

        public void Reset(ReadOnlyMemory<byte> value, DateTimeOffset createdAt, DateTimeOffset? expiresAt)
        {
            Value = value;
            CreatedAt = createdAt;
            ExpiresAt = expiresAt;
            Interlocked.Exchange(ref _hits, 0);
        }

        public bool IsExpired()
        {
            return IsExpired(DateTimeOffset.UtcNow);
        }
    }

    private Entry AcquireEntry(ReadOnlyMemory<byte> value, DateTimeOffset createdAt, DateTimeOffset? expiresAt)
    {
        if (_entryPool.TryTake(out var entry))
        {
            entry.Reset(value, createdAt, expiresAt);
            return entry;
        }

        return new Entry(value, createdAt, expiresAt);
    }

    private void ReleaseEntry(Entry entry)
    {
        _entryPool.Add(entry);
    }
}
