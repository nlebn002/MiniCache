# MiniCache

MiniCache is a lightweight .NET cache solution composed of:

- `MiniCache.Core`: a thread-safe in-memory cache that stores binary payloads, tracks TTL metadata, and exposes `ICache`.
- `MiniCache.Server`: an ASP.NET Core Web API that exposes cache operations over `/api/v1/cache/*`, includes health and swagger docs, and layers business logic through `ICacheManager`.
- `MiniCache.Benchmarks`: a BenchmarkDotNet harness that exercises writes, hits, misses, and metadata lookups against the cache under configurable load.

## Running the server

```bash
# restore + build once
dotnet build MiniCache.slnx

# run the API (defaults to http://localhost:5000 and https://localhost:5001)
dotnet run --project MiniCache.Server
```

The API exposes swagger at `/swagger` and a health endpoint at `/healthz`. Use `/api/v1/cache` to write binary data (Base64) and `/api/v1/cache/{key}` to read it back.

## API highlights

- `POST /api/v1/cache` — insert/update Base64 payloads with optional `"ttlSeconds"`.
- `GET /api/v1/cache/{key}` — return the cached value as Base64.
- `GET /api/v1/cache/{key}/metadata` — shows creation, expiration, and hit count.
- `DELETE /api/v1/cache/{key}` / `POST /api/v1/cache/clear` / `GET /api/v1/cache/count` provide management hooks.

Requests are validated with controller-model validation; invalid Base64 values return `400 Bad Request`.

## Running benchmarks

```bash
dotnet run --project MiniCache.Benchmarks -c Release
```

BenchmarkDotNet reports are written to `BenchmarkDotNet.Artifacts`.

## Notes

- In-memory cache uses pooling and `ConcurrentDictionary` so lookups and updates are lock-free and TTL-aware.
- The server is layered: controllers → `ICacheManager` → `MiniCache.Core.InMemoryCache`, making future extensions (auth, persistence, telemetry) easier.
