using BenchmarkDotNet.Running;

namespace MiniCache.Benchmarks;

public static class Program
{
    public static void Main() => BenchmarkRunner.Run<InMemoryCacheBenchmarks>();
}
