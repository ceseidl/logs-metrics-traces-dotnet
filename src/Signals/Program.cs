using Signals;

using var telemetry = new Telemetry();
var apps = await Services.StartAsync(telemetry);
using var client = new HttpClient();

// 200 checkouts before the deploy and 200 after it.
await SendAsync(1, 200);
Services.PaymentsVersion = "1.5.0";
await SendAsync(201, 400);

Report.Metrics(telemetry);
Report.Logs(telemetry);
Report.Trace(telemetry);

foreach (var app in apps)
    await app.StopAsync();

Task SendAsync(int from, int to) =>
    Parallel.ForEachAsync(
        Enumerable.Range(from, to - from + 1),
        new ParallelOptions { MaxDegreeOfParallelism = 20 },
        async (id, ct) =>
        {
            using var r = await client.PostAsync(
                $"{Services.Gateway}/checkout/{id}", null, ct);
        });
