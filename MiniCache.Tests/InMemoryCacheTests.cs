using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MiniCache.Core;
using Xunit;

namespace MiniCache.Tests;

public class InMemoryCacheTests
{
    [Fact]
    public void SetAndGetReturnsStoredValue()
    {
        using var cache = new InMemoryCache();

        var payload = new byte[] { 0x01, 0x02, 0x03 };
        cache.Set("key", payload);

        Assert.True(cache.TryGet("key", out var cached));
        Assert.Equal(payload, cached.ToArray());
        Assert.True(cache.TryGetMetadata("key", out var metadata));
        Assert.Equal(1, metadata.Hits);
        Assert.Null(metadata.ExpiresAt);
    }

    [Fact]
    public void MetadataReflectsHitsAndTtl()
    {
        using var cache = new InMemoryCache();

        cache.Set("target", new byte[] { 0xAB }, TimeSpan.FromSeconds(10));
        Assert.True(cache.TryGet("target", out _));
        Assert.True(cache.TryGetMetadata("target", out var metadata));

        Assert.Equal(1, metadata.Hits);
        Assert.NotNull(metadata.ExpiresAt);
        Assert.True(metadata.CreatedAt <= metadata.ExpiresAt);
    }

    [Fact]
    public void ExpiredEntryIsRemovedAutomatically()
    {
        using var cache = new InMemoryCache();

        cache.Set("temp", new byte[] { 0xFF }, TimeSpan.FromMilliseconds(30));
        Thread.Sleep(60);

        Assert.False(cache.TryGet("temp", out _));
        Assert.False(cache.TryGetMetadata("temp", out _));
    }

    [Fact]
    public void ClearAndRemoveDrainEntries()
    {
        using var cache = new InMemoryCache();

        cache.Set("first", new byte[] { 0x10 });
        cache.Set("second", new byte[] { 0x20 });

        Assert.True(cache.Remove("first"));
        Assert.False(cache.TryGet("first", out _));

        cache.Clear();
        Assert.False(cache.TryGet("second", out _));
        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public void ConcurrentWritesDoNotThrow()
    {
        using var cache = new InMemoryCache();
        var payload = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();

        Parallel.For(0, 1000, i =>
        {
            cache.Set($"key-{i % 10}", payload);
            cache.TryGet($"key-{i % 10}", out _);
        });

        Assert.True(cache.TryGet("key-5", out var retrieved));
        Assert.Equal(payload, retrieved.ToArray());
    }
}
