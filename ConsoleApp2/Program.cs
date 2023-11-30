
using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Sinks.OpenTelemetry;

var sumoUrl = "https://collectors.au.sumologic.com/receiver/v1/otlp/...";

var activitySource = new ActivitySource("TestApp");

using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddSource(activitySource.Name)
    .ConfigureResource(resource => resource.AddService(serviceName: "MyService", serviceVersion: "1.0.0"))
    .AddOtlpExporter(
        options =>
        {
            options.Endpoint = new Uri($"{sumoUrl}/v1/traces");
            options.Protocol = OtlpExportProtocol.HttpProtobuf;
        })
    .Build();

var log = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .WriteTo.OpenTelemetry(o =>
    {
        o.Endpoint = "http://localhost:5341/ingest/otlp/v1/logs";
        o.Protocol = OtlpProtocol.HttpProtobuf;
    })
    .WriteTo.OpenTelemetry(o =>
    {
        o.Endpoint =
            $"{sumoUrl}/v1/logs";
        o.Protocol = OtlpProtocol.HttpProtobuf;
    })
    .WriteTo.Console()
    .CreateLogger();


for(int x = 0; x < 100; x++)
{
    using var activity = activitySource.StartActivity("Log things out");
    
    
    log.Verbose("Verbose Message {X}", x);
    log.Debug("Debug Message {X}", x);
    log.Information("Information Message {X}", x);
    log.Warning("Warning Message {X}", x);

    using (var errorActivity = activitySource.StartActivity("Something is going to go wrong"))
    {
        errorActivity?.AddTag("Y", 42);
        log.Error("Error Message {X}", x);
        Thread.Sleep(1000);
        errorActivity?.AddEvent(new ActivityEvent("Fatal Event"));
        log.Fatal("Fatal Message {X}", x);
        Thread.Sleep(1000);
        log.Error(new Exception("Something bad"), "Exception Message {X}", x);
        Thread.Sleep(1000);
    }

}