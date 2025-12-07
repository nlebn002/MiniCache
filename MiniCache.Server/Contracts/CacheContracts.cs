using System;
using System.ComponentModel.DataAnnotations;
using MiniCache.Core;

namespace MiniCache.Server.Contracts;

/// <summary>
/// Request payload used to insert or update cache entries.
/// </summary>
public sealed record CacheUpsertRequest
{
    [Required]
    [MinLength(1)]
    public string Key { get; init; } = string.Empty;

    [Required]
    [MinLength(1)]
    public string ValueBase64 { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int? TtlSeconds { get; init; }
}

/// <summary>
/// Standard response from a successful cache write.
/// </summary>
public sealed record CacheSetResponse(string Key);

/// <summary>
/// Payload returned when reading a cache entry value.
/// </summary>
public sealed record CacheValueResponse(string Key, string ValueBase64);

/// <summary>
/// Payload returned when reading cache entry metadata.
/// </summary>
public sealed record CacheMetadataResponse(string Key, DateTimeOffset CreatedAt, DateTimeOffset? ExpiresAt, long Hits);

/// <summary>
/// Payload used by informational endpoints.
/// </summary>
public sealed record CacheCountResponse(long Count);

/// <summary>
/// Generic single-property response for operations such as clearing the cache.
/// </summary>
public sealed record CacheMessageResponse(string Message);
