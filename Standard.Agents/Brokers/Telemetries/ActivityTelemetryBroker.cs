// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Standard.Agents.Brokers.Telemetries;

/// <summary>
/// The in-box telemetry broker: OpenTelemetry-compatible spans and metrics through the BCL's
/// <see cref="ActivitySource"/> and <see cref="Meter"/> — no packages, no exporter, no
/// dependency. A process that wires an OpenTelemetry SDK (or any ActivityListener) against the
/// <c>Standard.Agents</c> source picks everything up; a process that wires nothing pays nothing,
/// because an unobserved source hands back <c>null</c> instead of a span. Attribute and metric
/// names follow the OpenTelemetry GenAI semantic conventions, so a collector that already
/// understands agents understands this one.
/// </summary>
public sealed class ActivityTelemetryBroker : ITelemetryBroker
{
    public const string SourceName = "Standard.Agents";

    private static readonly ActivitySource activitySource = new(SourceName);
    private static readonly Meter meter = new(SourceName);

    private static readonly Histogram<long> tokenUsage =
        meter.CreateHistogram<long>("gen_ai.client.token.usage", unit: "{token}");

    private static readonly Histogram<double> operationDuration =
        meter.CreateHistogram<double>("gen_ai.client.operation.duration", unit: "s");

    private readonly string agentName;

    public ActivityTelemetryBroker(string agentName = "standard-agent") =>
        this.agentName = agentName;

    public IDisposable? StartRun(string sessionId)
    {
        Activity? run = activitySource.StartActivity($"invoke_agent {this.agentName}");
        run?.SetTag("gen_ai.operation.name", "invoke_agent");
        run?.SetTag("gen_ai.agent.name", this.agentName);

        if (string.IsNullOrEmpty(sessionId) is false)
        {
            run?.SetTag("gen_ai.conversation.id", sessionId);
        }

        return run;
    }

    public IDisposable? StartTurn(int turn)
    {
        Activity? turnActivity = activitySource.StartActivity($"turn {turn}");
        turnActivity?.SetTag("standard.agents.turn.index", turn);

        return turnActivity;
    }

    public void RecordTurnUsage(int promptTokens, int completionTokens, bool estimated)
    {
        // Only a span this source opened is annotated — the ambient current activity may belong
        // to the host when nothing is listening here, and another library's span is not ours to
        // write on.
        Activity? current = Activity.Current;

        if (current?.Source == activitySource)
        {
            current.SetTag("gen_ai.usage.input_tokens", promptTokens);
            current.SetTag("gen_ai.usage.output_tokens", completionTokens);
            current.SetTag("standard.agents.usage.estimated", estimated);
        }

        tokenUsage.Record(
            promptTokens,
            new KeyValuePair<string, object?>("gen_ai.token.type", "input"));

        tokenUsage.Record(
            completionTokens,
            new KeyValuePair<string, object?>("gen_ai.token.type", "output"));
    }

    public void RecordRunOutcome(string status, int promptTokens, int completionTokens)
    {
        Activity? current = Activity.Current;

        if (current?.Source == activitySource)
        {
            current.SetTag("standard.agents.run.status", status);
            current.SetTag("gen_ai.usage.input_tokens", promptTokens);
            current.SetTag("gen_ai.usage.output_tokens", completionTokens);

            operationDuration.Record(
                (DateTime.UtcNow - current.StartTimeUtc).TotalSeconds,
                new KeyValuePair<string, object?>("gen_ai.operation.name", "invoke_agent"),
                new KeyValuePair<string, object?>("standard.agents.run.status", status));
        }
    }
}
