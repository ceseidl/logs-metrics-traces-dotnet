using System.Diagnostics;

namespace Signals;

public static class Report
{
    // Metrics: how is the system behaving?
    public static void Metrics(Telemetry t)
    {
        Console.WriteLine(
            "metrics: http.server.request.duration");
        Console.WriteLine(
            "service   version  requests  5xx    p50     p95");
        var groups = t.Metrics
            .GroupBy(m => (m.Service, m.Version))
            .OrderBy(g => g.Key);
        foreach (var g in groups)
        {
            var ms = g.Select(m => m.Ms).Order().ToArray();
            var errors = g.Count(m => m.Status >= 500);
            Console.WriteLine(
                $"{g.Key.Service,-9} {g.Key.Version,-8} " +
                $"{g.Count(),8} {errors,4} " +
                $"{P(ms, 0.50),6:0} ms {P(ms, 0.95),4:0} ms");
        }
        Console.WriteLine();
    }

    // Logs: what happened?
    public static void Logs(Telemetry t)
    {
        var errors = t.Logs
            .Where(l => l.Level == LogLevel.Error)
            .OrderBy(l => l.Message, StringComparer.Ordinal)
            .ToList();
        Console.WriteLine(
            $"logs: {t.Logs.Count} lines, " +
            $"{errors.Count} errors");
        foreach (var l in errors.Take(2))
            Console.WriteLine(
                $"  error {l.Service} \"{l.Message}\"\n" +
                $"        trace={l.TraceId}");
        Console.WriteLine();
    }

    // Traces: what path did the request take?
    public static void Trace(Telemetry t)
    {
        var traceId = t.Logs
            .Where(l => l.Level == LogLevel.Error)
            .OrderBy(l => l.Message, StringComparer.Ordinal)
            .First().TraceId;
        var spans = t.Spans
            .Where(s => s.TraceId.ToString() == traceId)
            .ToList();
        Console.WriteLine($"trace {traceId}");
        var root = spans.Single(
            s => spans.All(p => p.SpanId != s.ParentSpanId));
        Print(root, spans, 1, "load");
    }

    static void Print(
        Activity span, List<Activity> all, int depth,
        string caller)
    {
        // Spans without a service tag belong to the caller.
        var service =
            span.GetTagItem("service") as string ?? caller;
        var name = span.Kind == ActivityKind.Client
            ? $"call {new Uri((string)span.GetTagItem(
                "url.full")!).AbsolutePath}"
            : span.DisplayName;
        var error = span.Status == ActivityStatusCode.Error
            || span.GetTagItem("error.type") is not null
            ? "  ERROR" : "";
        var indent = new string(' ', depth * 2);
        Console.WriteLine(
            $"{indent + service + ": " + name,-44}" +
            $"{span.Duration.TotalMilliseconds,5:0} ms{error}");
        foreach (var child in all
            .Where(c => c.ParentSpanId == span.SpanId)
            .OrderBy(c => c.StartTimeUtc))
            Print(child, all, depth + 1, service);
    }

    static double P(double[] sorted, double p) =>
        sorted[(int)Math.Ceiling(p * sorted.Length) - 1];
}
