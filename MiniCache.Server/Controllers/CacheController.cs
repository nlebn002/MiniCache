using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MiniCache.Core;
using MiniCache.Server.Application;
using MiniCache.Server.Contracts;

namespace MiniCache.Server.Controllers;

[ApiController]
[Route("api/v1/cache")]
public sealed class CacheController : ControllerBase
{
    private readonly ICacheManager _cacheManager;
    private readonly ILogger<CacheController> _logger;

    public CacheController(ICacheManager cacheManager, ILogger<CacheController> logger)
    {
        _cacheManager = cacheManager;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpsertEntry(CacheUpsertRequest request)
    {
        if (!TryDecodeBase64(request.ValueBase64, out var payload))
        {
            ModelState.AddModelError(nameof(request.ValueBase64), "Value must be valid Base64.");
            return ValidationProblem();
        }

        await _cacheManager.SetAsync(request.Key, payload, request.TtlSeconds is { } ttl ? TimeSpan.FromSeconds(ttl) : null);
        _logger.LogInformation("Stored cache entry {Key}.", request.Key);

        return CreatedAtAction(nameof(GetEntryValue), new { key = request.Key }, new CacheSetResponse(request.Key));
    }

    [HttpGet("{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEntryValue(string key)
    {
        var (found, value) = await _cacheManager.TryGetAsync(key);
        if (!found)
        {
            return NotFound();
        }

        var response = new CacheValueResponse(key, Convert.ToBase64String(value.Span));
        return Ok(response);
    }

    [HttpGet("{key}/metadata")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEntryMetadata(string key)
    {
        var (found, metadata) = await _cacheManager.TryGetMetadataAsync(key);
        if (!found || metadata is null)
        {
            return NotFound();
        }

        return Ok(ToMetadataResponse(key, metadata.Value));
    }

    [HttpDelete("{key}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEntry(string key)
    {
        var removed = await _cacheManager.RemoveAsync(key);
        return removed ? NoContent() : NotFound();
    }

    [HttpGet("count")]
    public async Task<IActionResult> GetEntryCount()
    {
        var count = await _cacheManager.GetCountAsync();
        return Ok(new CacheCountResponse(count));
    }

    [HttpPost("clear")]
    public async Task<IActionResult> ClearCache()
    {
        await _cacheManager.ClearAsync();
        return Ok(new CacheMessageResponse("Cache cleared."));
    }

    private static CacheMetadataResponse ToMetadataResponse(string key, CacheEntryMetadata metadata)
    {
        return new CacheMetadataResponse(key, metadata.CreatedAt, metadata.ExpiresAt, metadata.Hits);
    }

    private static bool TryDecodeBase64(string value, [NotNullWhen(true)] out byte[]? decoded)
    {
        try
        {
            decoded = Convert.FromBase64String(value);
            return true;
        }
        catch (FormatException)
        {
            decoded = null;
            return false;
        }
    }
}
