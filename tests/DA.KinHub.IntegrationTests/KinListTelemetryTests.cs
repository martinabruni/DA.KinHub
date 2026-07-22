using System.Diagnostics;
using System.Diagnostics.Metrics;
using DA.KinHub.Functions.Configuration;
using DA.KinHub.Functions.Observability;
using Microsoft.Extensions.Options;

namespace DA.KinHub.IntegrationTests;

public sealed class KinListTelemetryTests
{
    [Fact]
    public void CompleteEmitsExactlyOneOutcomeAndDuration()
    {
        var longMeasurements = new List<(string Instrument, long Value, string? Operation, string? Outcome)>();
        var doubleMeasurements = new List<(string Instrument, double Value, string? Operation, string? Outcome)>();
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == "KinHub.KinList")
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
            longMeasurements.Add((instrument.Name, value, TagValue(tags, "operation"), TagValue(tags, "outcome"))));
        meterListener.SetMeasurementEventCallback<double>((instrument, value, tags, _) =>
            doubleMeasurements.Add((instrument.Name, value, TagValue(tags, "operation"), TagValue(tags, "outcome"))));
        meterListener.Start();

        var activities = new List<Activity>();
        using var activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "KinHub.KinList",
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => activities.Add(activity)
        };
        ActivitySource.AddActivityListener(activityListener);

        using var telemetry = new KinListTelemetry(new BuildInfoProvider(Options.Create(new RuntimeOptions { AppName = "KinHub", ApiVersion = "1.0", Environment = "Test" })));
        using (var operation = telemetry.Begin(KinListOperations.Bootstrap))
        {
            operation.Complete("family");
        }

        Assert.Single(longMeasurements);
        Assert.Single(doubleMeasurements);
        Assert.Equal("kinlist.outcomes", longMeasurements[0].Instrument);
        Assert.Equal(KinListOperations.Bootstrap, longMeasurements[0].Operation);
        Assert.Equal("family", longMeasurements[0].Outcome);
        Assert.Equal("kinlist.duration.ms", doubleMeasurements[0].Instrument);
        Assert.Equal(KinListOperations.Bootstrap, doubleMeasurements[0].Operation);
        Assert.Equal("family", doubleMeasurements[0].Outcome);

        var activity = Assert.Single(activities);
        Assert.Equal(KinListOperations.Bootstrap, activity.OperationName);
        Assert.Equal(ActivityStatusCode.Ok, activity.Status);
        Assert.Contains(activity.Tags, tag => tag.Key == "operation" && tag.Value == KinListOperations.Bootstrap);
        Assert.Contains(activity.Tags, tag => tag.Key == "outcome" && tag.Value == "family");
    }

    [Fact]
    public void RecordSignalEmitsOneLowCardinalityMeasurement()
    {
        var longMeasurements = new List<(string Instrument, long Value, string? Operation, string? Outcome, string? ErrorCategory)>();
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == "KinHub.KinList")
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
            longMeasurements.Add((
                instrument.Name,
                value,
                TagValue(tags, "operation"),
                TagValue(tags, "outcome"),
                TagValue(tags, "errorCategory"))));
        meterListener.Start();

        using var telemetry = new KinListTelemetry(new BuildInfoProvider(Options.Create(new RuntimeOptions { AppName = "KinHub", ApiVersion = "1.0", Environment = "Test" })));
        telemetry.RecordSignal(KinListOperations.ApiAccess, "auth.requiredClaims", "identity");

        var measurement = Assert.Single(longMeasurements, measurement => measurement.Instrument == "kinlist.signals");
        Assert.Equal(KinListOperations.ApiAccess, measurement.Operation);
        Assert.Equal("auth.requiredClaims", measurement.Outcome);
        Assert.Equal("identity", measurement.ErrorCategory);
    }

    private static string? TagValue(ReadOnlySpan<KeyValuePair<string, object?>> tags, string key)
    {
        foreach (var tag in tags)
        {
            if (tag.Key == key)
            {
                return tag.Value?.ToString();
            }
        }

        return null;
    }
}
