English | [Português](README.pt-BR.md)

# Logs, metrics and traces in .NET

Code for the article "Logs, métricas e traces em .NET: três visões do mesmo incidente".

Three ASP.NET Core services (`gateway` → `orders` → `payments`) run in one process. Halfway through, `payments` is deployed from 1.4.0 to 1.5.0, and the new version has a slow fraud check. The program follows the incident through the three signals:

- **Metrics**: `http.server.request.duration`, tagged with service name and version.
- **Logs**: every line carries the current trace id.
- **Traces**: the span tree of the failed request, across the three services.

No NuGet packages: the signals are read with `ActivityListener` and `MeterListener`. In production, use OpenTelemetry instead.

## Requirements

- .NET 10 SDK

## Run

```bash
dotnet run --project src/Signals
```

## Expected output (latencies vary a little)

```text
metrics: http.server.request.duration
service   version  requests  5xx    p50     p95
gateway   2.1.0         400   10     46 ms  208 ms
orders    3.0.2         400   10     45 ms  108 ms
payments  1.4.0         200    0     30 ms   43 ms
payments  1.5.0         200   10     31 ms  807 ms

logs: 400 lines, 10 errors
  error payments "Fraud check timed out for order 220"
        trace=b6cc653596e1ddbc4eebb2f2e6493937
  ...

trace b6cc653596e1ddbc4eebb2f2e6493937
  load: call /checkout/220                    825 ms  ERROR
    gateway: POST /checkout/{id:int}          824 ms  ERROR
      ...
              payments: fraud-check           812 ms  ERROR
```

## Structure

- `Program.cs`: starts the services, sends 200 checkouts before and 200 after the deploy, prints the report.
- `Services.cs`: the three services, the middleware that tags name and version, the fraud check span.
- `Telemetry.cs`: in-memory collection of spans, metrics and logs.
- `Report.cs`: metrics table, error logs and span tree.
