// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

// The same agent definition as a service. Five lines on a laptop, the same five lines
// behind HTTP: composition here is configuration, never new concepts - the appliance
// guarantee holds at the exposure layer too.

using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Standard.Agents;
using Standard.Agents.Brokers.Telemetries;
using Standard.Agents.Host.Security;
using Standard.Agents.Models.Clients.Agents.Exceptions;

const string AgentHttpClientName = "standard-agent";

// A validate-only run: `--validate [path]` composes the document and says whether it composes,
// naming the entry when it does not, without standing the host up - the check a deployment
// pipeline runs before a green heartbeat could mislead it (principal review 2026-09-04, F-24).
if (args.Contains("--validate"))
{
    string documentPath =
        args.SkipWhile(argument => argument != "--validate").Skip(1).FirstOrDefault()
            ?? Path.Combine(AppContext.BaseDirectory, "agent.json");

    try
    {
        StandardAgent.FromJson(File.ReadAllText(documentPath));
        Console.WriteLine($"{documentPath} composes.");

        return 0;
    }
    catch (Exception exception) when (exception is InvalidAgentConfigurationException or IOException)
    {
        Console.Error.WriteLine($"{documentPath} does not compose: {exception.Message}");

        return 1;
    }
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// The agent's HTTP traffic - brain, native brain, MCP servers - rides handlers the host owns,
// pooled and DNS-refreshing through IHttpClientFactory rather than clients each broker built
// for itself (principal review 2026-09-04, F-23). One named registration is the one place to
// put a proxy, a certificate, a resilience handler or an observer under all of it.
builder.Services.AddHttpClient(AgentHttpClientName);

// Exporting is opt-in by the standard OTel switch: set OTEL_EXPORTER_OTLP_ENDPOINT and the
// agent's spans and metrics (plus the HTTP server's) leave for your collector; leave it unset
// and nothing is wired, so the default host stays exactly what it was. The library side needs
// no flag at all - an unobserved ActivitySource already costs nothing.
if (string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]) is false)
{
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService(
            builder.Configuration["Agent:Name"] ?? "standard-agents-host"))
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation()
            .AddSource(ActivityTelemetryBroker.SourceName)
            .AddOtlpExporter())
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddMeter(ActivityTelemetryBroker.SourceName)
            .AddOtlpExporter());
}

// One agent, one singleton - one instance serving prompts concurrently is the intended
// shape (docs/support.md), and run state is per invocation by SPEC.md 4.4. Zero config
// must still run: without a Brain the host stands, heartbeats, and answers with what to
// configure rather than crashing at startup.
builder.Services.AddSingleton<IAgent>(provider =>
{
    IConfiguration configuration = provider.GetRequiredService<IConfiguration>();

    IHttpMessageHandlerFactory httpHandlers =
        provider.GetRequiredService<IHttpMessageHandlerFactory>();

    // The agent as data: Agent:Config names a JSON document, or an agent.json beside the
    // executable is picked up on its own — a low-code platform writes a file and has an agent,
    // no C# anywhere. When the document is the source, the document is the whole truth: skills,
    // telemetry, guardians and budgets all come from its keys, not from a second config source
    // that could quietly disagree with it.
    string configuredPath = configuration["Agent:Config"] ?? string.Empty;

    string agentJsonPath = string.IsNullOrWhiteSpace(configuredPath)
        ? Path.Combine(AppContext.BaseDirectory, "agent.json")
        : configuredPath;

    if (string.IsNullOrWhiteSpace(configuredPath) is false || File.Exists(agentJsonPath))
    {
        string agentJson = File.ReadAllText(agentJsonPath);
        StandardAgent configured = StandardAgent.FromJson(agentJson);

        // One instance serves every caller, so a memory would be one memory for all of them:
        // one caller's facts in another caller's context. The document must name a memory to
        // get one; otherwise the agent recalls nothing and offers no way to store
        // (principal review 2026-09-04, F-05).
        bool namesMemory =
            System.Text.Json.Nodes.JsonNode.Parse(agentJson) is System.Text.Json.Nodes.JsonObject memoryDocument
                && memoryDocument.ContainsKey("memory");

        if (namesMemory is false)
        {
            configured.WithoutMemory();
        }

        // The same zero-config grace the classic path has: a document with no brain still
        // stands, heartbeats, and answers with what to add — never a 500 at the first prompt.
        bool hasBrain =
            System.Text.Json.Nodes.JsonNode.Parse(agentJson) is System.Text.Json.Nodes.JsonObject document
                && (document.ContainsKey("brain")
                    || document.ContainsKey("nativeBrain")
                    || document.ContainsKey("nativeBrainAnthropic"));

        if (hasBrain is false)
        {
            configured.OnBrain(async (_, _) =>
                "FINAL: No Brain is configured. Add \"brain\", \"nativeBrain\" or "
                    + "\"nativeBrainAnthropic\" to agent.json, then restart the host.");
        }

        return configured.Http(() => httpHandlers.CreateHandler(AgentHttpClientName));
    }

    string? url = configuration["Agent:Url"];

    StandardAgent agent = string.IsNullOrWhiteSpace(url)
        ? new StandardAgent().OnBrain(async (_, _) =>
            "FINAL: No Brain is configured. Provide an agent.json (or set Agent:Config), or "
                + "set Agent:Url, Agent:ApiKey and Agent:Model, then restart the host.")
        : new StandardAgent(
            url,
            configuration["Agent:ApiKey"] ?? string.Empty,
            configuration["Agent:Model"] ?? string.Empty);

    string? skillsPath = configuration["Agent:Skills"];

    if (string.IsNullOrWhiteSpace(skillsPath) is false)
    {
        agent.Skills(skillsPath);
    }

    // The same rule as the document: a hosted memory is shared by every caller, so it exists
    // only when the configuration names it (Agent:Memory).
    string? memoryPath = configuration["Agent:Memory"];

    if (string.IsNullOrWhiteSpace(memoryPath))
    {
        agent.WithoutMemory();
    }
    else
    {
        agent.Memory(memoryPath);
    }

    // Always on: the spans exist only when something listens, so this line is free on a
    // laptop and load-bearing behind a collector.
    agent.Telemetry(configuration["Agent:Name"] ?? "standard-agent");

    return agent.Http(() => httpHandlers.CreateHandler(AgentHttpClientName));
});

var app = builder.Build();

// The front door, before the routes: no configured Host:ApiKey means open (a laptop), one
// configuration line means every agent route wants X-Api-Key (a deployment). The heartbeat
// stays open either way - a probe cannot present a key and learns nothing.
app.Use(async (context, next) =>
{
    bool allowed = ApiKeyGate.Allows(
        configuredKey: app.Configuration["Host:ApiKey"],
        presentedKey: context.Request.Headers["X-Api-Key"],
        path: context.Request.Path);

    if (allowed)
    {
        await next();

        return;
    }

    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
    await context.Response.WriteAsync("missing or invalid X-Api-Key");
});

app.MapControllers();

app.Run();

return 0;

// Top-level statements compile to an internal Program; the acceptance tests stand the host up
// through WebApplicationFactory<Program>, which needs to see it (F-20).
public partial class Program { }
