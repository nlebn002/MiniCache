using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MiniCache.Core;

namespace MiniCache.Server.Application;

internal sealed class CacheManager : ICacheManager
{
    private readonly ICache _cache;
    private readonly ILogger<CacheManager> _logger;

    public CacheManager(ICache cache, ILogger<CacheManager> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public ValueTask SetAsync(string key, ReadOnlyMemory<byte> value, TimeSpan? ttl)
    {
        _cache.Set(key, value, ttl);
        _logger.LogDebug("Set cache entry {Key} with TTL {TTL}.", key, ttl);
        return ValueTask.CompletedTask;
    }

    public ValueTask<(bool Found, byte[]? Value)> TryGetAsync(string key)
    {
        if (_cache.TryGet(key, out var raw))
        {
            return ValueTask.FromResult<(bool Found, byte[]? Value)>((true, raw.ToArray()));
        }

        return new ValueTask<(bool Found, byte[]? Value)>((false, null));
    }

    public ValueTask<(bool Found, CacheEntryMetadata? Metadata)> TryGetMetadataAsync(string key)
    {
        if (_cache.TryGetMetadata(key, out var metadata))
        {
            return new ValueTask<(bool Found, CacheEntryMetadata? Metadata)>((true, metadata));
        }

        return new ValueTask<(bool Found, CacheEntryMetadata? Metadata)>((false, null));
    }

    public ValueTask<bool> RemoveAsync(string key)
    {
        return new ValueTask<bool>(_cache.Remove(key));
    }

    public ValueTask<long> GetCountAsync()
    {
        return new ValueTask<long>(_cache.Count);
    }

    public ValueTask ClearAsync()
    {
        _cache.Clear();
        _logger.LogInformation("Clear cache request executed; entry count is now zero.");
        return ValueTask.CompletedTask;
    }
}
