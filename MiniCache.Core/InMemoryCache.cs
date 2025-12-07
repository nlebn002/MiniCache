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
        if (!_entries.TryGetValue(key, out var entry))
        {
            value = default;
            return false;
        }

        var nowTicks = DateTime.UtcNow.Ticks;
        if (entry.IsExpired(nowTicks))
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

        if (!_entries.TryGetValue(key, out var entry))
            return false;

        var nowTicks = DateTime.UtcNow.Ticks;
        if (entry.IsExpired(nowTicks))
        {
            if (_entries.TryRemove(key, out var expiredEntry))
            {
                ReleaseEntry(expiredEntry);
            }

            return false;
        }

        metadata = new CacheEntryMetadata(entry.CreatedAt, entry.ExpiresAt, entry.Hits);
        return true;
    }

    private void CleanupExpiredEntries()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var (key, entry) in _entries)
        {
            if (entry.IsExpired(now.UtcTicks))
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

    private sealed class Entry
    {
        private long _hits;
        public Entry(ReadOnlyMemory<byte> value, DateTimeOffset createdAt, DateTimeOffset? expiresAt)
        {
            Value = value;
            _createdTicks = createdAt.UtcTicks;
            _expiresTicks = expiresAt?.UtcTicks;
            _hits = 0;
        }

        public ReadOnlyMemory<byte> Value { get; private set; }

        private long _createdTicks;
        private long? _expiresTicks;

        public DateTimeOffset CreatedAt => new(_createdTicks, TimeSpan.Zero);

        public DateTimeOffset? ExpiresAt => _expiresTicks.HasValue ? new DateTimeOffset(_expiresTicks.Value, TimeSpan.Zero) : null;

        public long Hits => Interlocked.Read(ref _hits);

        public void IncreaseHits()
        {
            Interlocked.Increment(ref _hits);
        }

        public bool IsExpired(long nowTicks)
        {
            if (!_expiresTicks.HasValue)
            {
                return false;
            }

            return _expiresTicks.Value <= nowTicks;
        }

        public void Reset(ReadOnlyMemory<byte> value, DateTimeOffset createdAt, DateTimeOffset? expiresAt)
        {
            Value = value;
            _createdTicks = createdAt.UtcTicks;
            _expiresTicks = expiresAt?.UtcTicks;
            Interlocked.Exchange(ref _hits, 0);
        }

        public bool IsExpired()
        {
            return IsExpired(DateTimeOffset.UtcNow.UtcTicks);
        }
    }
}
