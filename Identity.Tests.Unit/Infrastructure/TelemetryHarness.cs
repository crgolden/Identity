namespace Identity.Tests.Unit.Infrastructure;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

internal sealed class TelemetryHarness : IDisposable
{
    private readonly ServiceProvider _services;
    private readonly ActivityListener _activityListener;
    private readonly ConcurrentQueue<Activity> _stoppedActivities = new();

    internal TelemetryHarness()
    {
        _services = new ServiceCollection().AddMetrics().BuildServiceProvider();
        MeterFactory = _services.GetRequiredService<IMeterFactory>();
        Descriptions = TestValues.NewTelemetryOptions();
        Telemetry = new Telemetry(MeterFactory, Options.Create(Descriptions));
        _activityListener = new ActivityListener
        {
            ShouldListenTo = source => ReferenceEquals(source, Telemetry.ActivitySource),
            Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = _stoppedActivities.Enqueue,
        };
        ActivitySource.AddActivityListener(_activityListener);
    }

    internal IReadOnlyCollection<Activity> StoppedActivities => _stoppedActivities;

    internal IMeterFactory MeterFactory { get; }

    internal TelemetryOptions Descriptions { get; }

    internal Telemetry Telemetry { get; }

    public void Dispose()
    {
        _activityListener.Dispose();
        Telemetry.Dispose();
        _services.Dispose();
    }

    internal Dictionary<string, string?> PublishedDescriptions()
    {
        Dictionary<string, string?> descriptions = new(StringComparer.Ordinal);
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, _) =>
        {
            if (ReferenceEquals(instrument.Meter.Scope, MeterFactory))
            {
                descriptions[instrument.Name] = instrument.Description;
            }
        };
        listener.Start();
        return descriptions;
    }

    internal MeterListener ListenTo(string instrumentName, Action<long, KeyValuePair<string, object?>[]> onMeasurement)
    {
        var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (ReferenceEquals(instrument.Meter.Scope, MeterFactory) &&
                string.Equals(instrument.Name, instrumentName, StringComparison.Ordinal))
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, value, tags, _) => onMeasurement(value, tags.ToArray()));
        listener.Start();
        return listener;
    }
}
