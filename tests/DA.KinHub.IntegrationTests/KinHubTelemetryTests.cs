using System.Diagnostics;
using System.Diagnostics.Metrics;
using DA.KinHub.Functions.Configuration;
using DA.KinHub.Functions.Observability;
using Microsoft.Extensions.Options;

namespace DA.KinHub.IntegrationTests;

public sealed class KinHubTelemetryTests
{
    [Fact]
    public void CompleteEmitsExactlyOneOutcomeAndDuration()
    {
        var longMeasurements = new List<(string Instrument, long Value, string? Operation, string? Outcome)>();
        var doubleMeasurements = new List<(string Instrument, double Value, string? Operation, string? Outcome)>();
        var measurementsLock = new object();
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == "KinHub")
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
        {
            lock (measurementsLock)
            {
                longMeasurements.Add((instrument.Name, value, TagValue(tags, "operation"), TagValue(tags, "outcome")));
            }
        });
        meterListener.SetMeasurementEventCallback<double>((instrument, value, tags, _) =>
        {
            lock (measurementsLock)
            {
                doubleMeasurements.Add((instrument.Name, value, TagValue(tags, "operation"), TagValue(tags, "outcome")));
            }
        });
        meterListener.Start();

        var activities = new List<Activity>();
        using var activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "KinHub",
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => activities.Add(activity)
        };
        ActivitySource.AddActivityListener(activityListener);

        using var telemetry = new KinHubTelemetry(new BuildInfoProvider(Options.Create(new RuntimeOptions { AppName = "KinHub", ApiVersion = "1.0", Environment = "Test" })));
        using (var operation = telemetry.Begin(KinHubOperations.Bootstrap))
        {
            operation.Complete("family");
        }

        var longMeasurement = Assert.Single(Snapshot(longMeasurements, measurementsLock));
        var doubleMeasurement = Assert.Single(Snapshot(doubleMeasurements, measurementsLock));
        Assert.Equal("kinhub.outcomes", longMeasurement.Instrument);
        Assert.Equal(KinHubOperations.Bootstrap, longMeasurement.Operation);
        Assert.Equal("family", longMeasurement.Outcome);
        Assert.Equal("kinhub.duration.ms", doubleMeasurement.Instrument);
        Assert.Equal(KinHubOperations.Bootstrap, doubleMeasurement.Operation);
        Assert.Equal("family", doubleMeasurement.Outcome);

        var activity = Assert.Single(activities);
        Assert.Equal(KinHubOperations.Bootstrap, activity.OperationName);
        Assert.Equal(ActivityStatusCode.Ok, activity.Status);
        Assert.Contains(activity.Tags, tag => tag.Key == "operation" && tag.Value == KinHubOperations.Bootstrap);
        Assert.Contains(activity.Tags, tag => tag.Key == "outcome" && tag.Value == "family");
    }

    [Fact]
    public void RecordSignalEmitsOneLowCardinalityMeasurement()
    {
        var longMeasurements = new List<(string Instrument, long Value, string? Operation, string? Outcome, string? ErrorCategory)>();
        var measurementsLock = new object();
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == "KinHub")
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
        {
            lock (measurementsLock)
            {
                longMeasurements.Add((
                    instrument.Name,
                    value,
                    TagValue(tags, "operation"),
                    TagValue(tags, "outcome"),
                    TagValue(tags, "errorCategory")));
            }
        });
        meterListener.Start();

        using var telemetry = new KinHubTelemetry(new BuildInfoProvider(Options.Create(new RuntimeOptions { AppName = "KinHub", ApiVersion = "1.0", Environment = "Test" })));
        telemetry.RecordSignal(KinHubOperations.ApiAccess, "auth.requiredClaims", "identity");

        var measurement = Assert.Single(Snapshot(longMeasurements, measurementsLock), measurement => measurement.Instrument == "kinhub.signals");
        Assert.Equal(KinHubOperations.ApiAccess, measurement.Operation);
        Assert.Equal("auth.requiredClaims", measurement.Outcome);
        Assert.Equal("identity", measurement.ErrorCategory);
    }

    [Fact]
    public void FamilyCreationSignalsStayLowCardinality()
    {
        var longMeasurements = new List<(string Instrument, long Value, string? Operation, string? Outcome, string? ErrorCategory)>();
        var measurementsLock = new object();
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == "KinHub")
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
        {
            lock (measurementsLock)
            {
                longMeasurements.Add((
                    instrument.Name,
                    value,
                    TagValue(tags, "operation"),
                    TagValue(tags, "outcome"),
                    TagValue(tags, "errorCategory")));
            }
        });
        meterListener.Start();

        using var telemetry = new KinHubTelemetry(new BuildInfoProvider(Options.Create(new RuntimeOptions { AppName = "KinHub", ApiVersion = "1.0", Environment = "Test" })));
        telemetry.RecordSignal(KinHubOperations.FamilyCreation, "attempt");
        telemetry.RecordSignal(KinHubOperations.FamilyCreation, "concurrent_conflict", "concurrency");
        telemetry.RecordSignal(KinHubOperations.FamilyCreation, "postgresql_unavailable", "dependency");

        var measurements = Snapshot(longMeasurements, measurementsLock);
        Assert.Contains(measurements, measurement => measurement.Instrument == "kinhub.signals"
            && measurement.Operation == KinHubOperations.FamilyCreation
            && measurement.Outcome == "attempt"
            && measurement.ErrorCategory is null);
        Assert.Contains(measurements, measurement => measurement.Instrument == "kinhub.signals"
            && measurement.Operation == KinHubOperations.FamilyCreation
            && measurement.Outcome == "concurrent_conflict"
            && measurement.ErrorCategory == "concurrency");
        Assert.Contains(measurements, measurement => measurement.Instrument == "kinhub.signals"
            && measurement.Operation == KinHubOperations.FamilyCreation
            && measurement.Outcome == "postgresql_unavailable"
            && measurement.ErrorCategory == "dependency");
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

    private static T[] Snapshot<T>(List<T> measurements, object gate)
    {
        lock (gate)
        {
            return measurements.ToArray();
        }
    }
}
