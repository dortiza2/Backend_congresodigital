using System.Collections.Concurrent;

namespace Congreso.Api.Services;

public interface IRequestMetrics
{
    void Observe(string route, long latencyMs);
    object GetSnapshot();
}

public class InMemoryRequestMetrics : IRequestMetrics
{
    private class MetricEntry
    {
        public long Count;
        public long TotalLatencyMs;
    }

    private readonly ConcurrentDictionary<string, MetricEntry> _metrics = new();

    public void Observe(string route, long latencyMs)
    {
        var entry = _metrics.GetOrAdd(route, _ => new MetricEntry());
        Interlocked.Increment(ref entry.Count);
        Interlocked.Add(ref entry.TotalLatencyMs, latencyMs);
    }

    public object GetSnapshot()
    {
        return _metrics.ToDictionary(
            kvp => kvp.Key,
            kvp => new
            {
                count = kvp.Value.Count,
                avgLatencyMs = kvp.Value.Count == 0 ? 0 : (double)kvp.Value.TotalLatencyMs / kvp.Value.Count
            }
        );
    }
}