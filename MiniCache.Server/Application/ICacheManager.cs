using System.Threading.Tasks;
using MiniCache.Core;

namespace MiniCache.Server.Application;

/// <summary>
/// Defines the higher-level cache operations that the API layer exposes via controllers.
/// </summary>
public interface ICacheManager
{
    ValueTask SetAsync(string key, ReadOnlyMemory<byte> value, TimeSpan? ttl);

    ValueTask<(bool Found, byte[]? Value)> TryGetAsync(string key);

    ValueTask<(bool Found, CacheEntryMetadata? Metadata)> TryGetMetadataAsync(string key);

    ValueTask<bool> RemoveAsync(string key);

    ValueTask<long> GetCountAsync();

    ValueTask ClearAsync();
}
