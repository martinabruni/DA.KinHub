using System.Diagnostics;
using System.Diagnostics.Metrics;
using DA.KinHub.Functions.Configuration;

namespace DA.KinHub.Functions.Observability;

public sealed class KinHubTelemetry : IDisposable
{
    private readonly Meter meter;
    private readonly Counter<long> outcomeCounter;
    private readonly Counter<long> signalCounter;
    private readonly Histogram<double> durationHistogram;
    private readonly Histogram<int> requestedPageSizeHistogram;
    private readonly Histogram<int> effectivePageSizeHistogram;
    private readonly ActivitySource activitySource = new("KinHub");

    public KinHubTelemetry(BuildInfoProvider buildInfoProvider)
    {
        meter = new Meter("KinHub", buildInfoProvider.Get().Version);
        outcomeCounter = meter.CreateCounter<long>("kinhub.outcomes");
        signalCounter = meter.CreateCounter<long>("kinhub.signals");
        durationHistogram = meter.CreateHistogram<double>("kinhub.duration.ms");
        requestedPageSizeHistogram = meter.CreateHistogram<int>("kinhub.pagination.requested_page_size");
        effectivePageSizeHistogram = meter.CreateHistogram<int>("kinhub.pagination.effective_page_size");
    }

    public OperationScope Begin(string operation) => new(operation, activitySource, outcomeCounter, durationHistogram);

    public void RecordSignal(string operation, string outcome, string? errorCategory = null)
    {
        var tags = CreateTags(operation, outcome, errorCategory);
        Activity.Current?.SetTag("operation", operation);
        Activity.Current?.SetTag("outcome", outcome);
        if (!string.IsNullOrWhiteSpace(errorCategory))
        {
            Activity.Current?.SetTag("errorCategory", errorCategory);
        }

        signalCounter.Add(1, tags);
    }

    public void RecordItemsPageRequest(int requestedPageSize, bool hasCursor)
    {
        var tags = new TagList
        {
            { "operation", KinHubOperations.KinListItemsPage },
            { "cursor", hasCursor ? "present" : "absent" }
        };
        requestedPageSizeHistogram.Record(requestedPageSize, tags);
    }

    public void RecordItemsPageResult(int effectivePageSize, bool hasPrevious, bool hasNext)
    {
        var tags = new TagList
        {
            { "operation", KinHubOperations.KinListItemsPage },
            { "hasPrevious", hasPrevious ? "true" : "false" },
            { "hasNext", hasNext ? "true" : "false" }
        };
        effectivePageSizeHistogram.Record(effectivePageSize, tags);
    }

    public void Dispose()
    {
        activitySource.Dispose();
        meter.Dispose();
    }

    public sealed class OperationScope : IDisposable
    {
        private readonly string operation;
        private readonly Counter<long> outcomeCounter;
        private readonly Histogram<double> durationHistogram;
        private readonly Stopwatch stopwatch = Stopwatch.StartNew();
        private readonly Activity? activity;
        private bool completed;

        public OperationScope(string operation, ActivitySource activitySource, Counter<long> outcomeCounter, Histogram<double> durationHistogram)
        {
            this.operation = operation;
            this.outcomeCounter = outcomeCounter;
            this.durationHistogram = durationHistogram;
            activity = activitySource.StartActivity(operation, ActivityKind.Internal);
        }

        public void Complete(string outcome)
        {
            if (completed)
            {
                return;
            }

            completed = true;
            Record(outcome, ActivityStatusCode.Ok);
        }

        public void Dispose()
        {
            if (!completed)
            {
                Record("incomplete", ActivityStatusCode.Error);
            }

            activity?.Dispose();
        }

        private void Record(string outcome, ActivityStatusCode status)
        {
            var tags = CreateTags(operation, outcome, errorCategory: null);
            activity?.SetStatus(status);
            activity?.SetTag("operation", operation);
            activity?.SetTag("outcome", outcome);
            outcomeCounter.Add(1, tags);
            durationHistogram.Record(stopwatch.Elapsed.TotalMilliseconds, tags);
        }
    }

    private static TagList CreateTags(string operation, string outcome, string? errorCategory)
    {
        var tags = new TagList { { "operation", operation }, { "outcome", outcome } };
        if (!string.IsNullOrWhiteSpace(errorCategory))
        {
            tags.Add("errorCategory", errorCategory);
        }

        return tags;
    }
}
