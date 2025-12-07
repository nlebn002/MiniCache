using BenchmarkDotNet.Attributes;
using MiniCache.Core;

namespace MiniCache.Benchmarks;

[MemoryDiagnoser]
public class InMemoryCacheBenchmarks
{
    private ICache _cache = null!;
    private ReadOnlyMemory<byte> _payload = Array.Empty<byte>();

    [Params(1, 100, 1000)]
    public int InitialEntries { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _cache = new InMemoryCache();
        var payload = new byte[64];
        Random.Shared.NextBytes(payload);
        _payload = payload;

        for (var i = 0; i < InitialEntries; i++)
        {
            _cache.Set($"bench-{i}", _payload, TimeSpan.FromSeconds(30));
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (_cache is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    [Benchmark]
    public void SetSingle()
    {
        _cache.Set("bench-single", _payload, TimeSpan.FromMinutes(5));
    }

    [Benchmark]
    public void TryGetExisting()
    {
        _cache.TryGet("bench-0", out _);
    }

    [Benchmark]
    public void TryGetMissing()
    {
        _cache.TryGet("does-not-exist", out _);
    }

    [Benchmark]
    public void MetadataAccess()
    {
        _cache.TryGetMetadata("bench-0", out _);
    }
}
