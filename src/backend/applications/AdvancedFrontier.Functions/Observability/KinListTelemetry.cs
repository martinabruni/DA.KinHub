using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace AdvancedFrontier.Functions.Observability;

public sealed class KinListTelemetry : IDisposable
{
    private readonly Meter meter = new("KinHub.KinList", "1.0.0");
    private readonly Counter<long> outcomeCounter;
    private readonly Histogram<double> durationHistogram;
    private readonly ActivitySource activitySource = new("KinHub.KinList");

    public KinListTelemetry()
    {
        outcomeCounter = meter.CreateCounter<long>("kinlist.outcomes");
        durationHistogram = meter.CreateHistogram<double>("kinlist.duration.ms");
    }

    public Activity? StartActivity(string operation) => activitySource.StartActivity(operation, ActivityKind.Internal);

    public void Track(string operation, string outcome, TimeSpan duration)
    {
        var tags = new TagList { { "operation", operation }, { "outcome", outcome } };
        outcomeCounter.Add(1, tags);
        durationHistogram.Record(duration.TotalMilliseconds, tags);
    }

    public void Dispose()
    {
        activitySource.Dispose();
        meter.Dispose();
    }
}
