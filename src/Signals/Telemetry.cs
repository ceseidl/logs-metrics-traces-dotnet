using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Signals;

public record LogEntry(
    LogLevel Level, string Service, string Message,
    string TraceId);

public record Measurement(
    string Service, string Version, int Status, double Ms);

// Collects the three signals in memory. In production an
// OpenTelemetry exporter sends them to a backend instead.
public sealed class Telemetry : IDisposable
{
    public ConcurrentBag<Activity> Spans { get; } = [];
    public ConcurrentBag<Measurement> Metrics { get; } = [];
    public ConcurrentBag<LogEntry> Logs { get; } = [];

    readonly ActivityListener _traces;
    readonly MeterListener _meters = new();

    public Telemetry()
    {
        _traces = new ActivityListener
        {
            ShouldListenTo = s => s.Name is
                "Microsoft.AspNetCore" or
                "System.Net.Http" or
                "Payments",
            Sample = (ref ActivityCreationOptions<
                ActivityContext> _) =>
                ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = Spans.Add,
        };
        ActivitySource.AddActivityListener(_traces);

        _meters.InstrumentPublished = (i, l) =>
        {
            if (i.Name == "http.server.request.duration")
                l.EnableMeasurementEvents(i);
        };
        _meters.SetMeasurementEventCallback<double>(
            (_, seconds, span, _) =>
            {
                var tags = span.ToArray();
                string Tag(string key) =>
                    tags.FirstOrDefault(t => t.Key == key)
                        .Value?.ToString() ?? "-";
                Metrics.Add(new(
                    Tag("service"), Tag("service.version"),
                    int.Parse(Tag("http.response.status_code")),
                    seconds * 1000));
            });
        _meters.Start();
    }

    public void Dispose()
    {
        _traces.Dispose();
        _meters.Dispose();
    }
}

// Writes every log line with the current trace id.
public sealed class MemoryLoggerProvider(
    Telemetry telemetry, string service) : ILoggerProvider
{
    public ILogger CreateLogger(string category) =>
        new MemoryLogger(telemetry, service);

    public void Dispose() { }

    sealed class MemoryLogger(Telemetry t, string service)
        : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel level) =>
            level >= LogLevel.Information;

        public void Log<TState>(
            LogLevel level, EventId id, TState state,
            Exception? error,
            Func<TState, Exception?, string> format)
        {
            var traceId =
                Activity.Current?.TraceId.ToString() ?? "-";
            t.Logs.Add(new(
                level, service, format(state, error), traceId));
        }
    }
}
