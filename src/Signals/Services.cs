using System.Diagnostics;
using Microsoft.AspNetCore.Http.Features;

namespace Signals;

public static class Services
{
    public const string Gateway = "http://127.0.0.1:5101";
    public const string Orders = "http://127.0.0.1:5102";
    public const string Payments = "http://127.0.0.1:5103";

    static readonly ActivitySource Source = new("Payments");

    // The payments version changes in the middle of the run.
    public static string PaymentsVersion = "1.4.0";

    public static async Task<WebApplication[]> StartAsync(
        Telemetry t)
    {
        var gateway =
            Create("gateway", Gateway, t, () => "2.1.0");
        gateway.MapPost("/checkout/{id:int}", (
            int id, IHttpClientFactory http) =>
            Forward(http, $"{Orders}/orders/{id}"));

        var orders = Create("orders", Orders, t, () => "3.0.2");
        orders.MapPost("/orders/{id:int}", async (
            int id, IHttpClientFactory http) =>
        {
            await Task.Delay(5); // save the order
            return await Forward(
                http, $"{Payments}/payments/{id}");
        });

        var payments = Create(
            "payments", Payments, t, () => PaymentsVersion);
        payments.MapPost("/payments/{id:int}", PayAsync);

        WebApplication[] apps = [gateway, orders, payments];
        foreach (var app in apps)
            await app.StartAsync();
        return apps;
    }

    static WebApplication Create(
        string name, string url, Telemetry t,
        Func<string> version)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls(url);
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(
            new MemoryLoggerProvider(t, name));
        builder.Logging.AddFilter("Microsoft", LogLevel.None);
        builder.Logging.AddFilter("System", LogLevel.None);
        builder.Services.AddHttpClient();

        var app = builder.Build();
        app.Use(async (ctx, next) =>
        {
            // Same name and version on the span and the metric.
            var v = version();
            var span = Activity.Current;
            span?.SetTag("service", name);
            span?.SetTag("service.version", v);
            var tags = ctx.Features
                .Get<IHttpMetricsTagsFeature>()?.Tags;
            tags?.Add(new("service", name));
            tags?.Add(new("service.version", v));

            var route = (ctx.GetEndpoint() as RouteEndpoint)
                ?.RoutePattern.RawText;
            if (span is not null)
                span.DisplayName =
                    $"{ctx.Request.Method} {route}";

            await next();
            if (ctx.Response.StatusCode >= 500)
                span?.SetStatus(ActivityStatusCode.Error);
        });
        return app;
    }

    static async Task<IResult> Forward(
        IHttpClientFactory http, string url)
    {
        using var response =
            await http.CreateClient().PostAsync(url, null);
        return Results.StatusCode((int)response.StatusCode);
    }

    static async Task<IResult> PayAsync(
        int id, ILoggerFactory logs)
    {
        var log = logs.CreateLogger("Payments");
        var bug = PaymentsVersion == "1.5.0" && id % 10 == 0;

        using (var span = Source.StartActivity("fraud-check"))
        {
            await Task.Delay(bug ? 800 : 20);
            if (bug && id % 20 == 0)
            {
                span?.SetStatus(ActivityStatusCode.Error);
                log.LogError(
                    "Fraud check timed out for order {OrderId}",
                    id);
                return Results.StatusCode(502);
            }
        }

        log.LogInformation(
            "Payment approved for order {OrderId}", id);
        return Results.Ok();
    }
}
