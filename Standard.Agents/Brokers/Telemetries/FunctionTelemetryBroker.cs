// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Brokers.Telemetries;

/// <summary>
/// The Custom mode of the telemetry seam: every boundary the loop crosses arrives at one
/// delegate as a named event with its attributes, for a pipeline no ActivityListener reaches —
/// StatsD, a log shipper, a metrics API of your own.
/// </summary>
public sealed class FunctionTelemetryBroker : ITelemetryBroker
{
    private readonly Action<string, IReadOnlyDictionary<string, object?>> record;

    public FunctionTelemetryBroker(
        Action<string, IReadOnlyDictionary<string, object?>> record) =>
        this.record = record;

    public IDisposable? StartRun(string sessionId)
    {
        this.record(
            "run.start",
            new Dictionary<string, object?> { ["session.id"] = sessionId });

        return new Scope(() => this.record("run.end", EmptyAttributes));
    }

    public IDisposable? StartTurn(int turn)
    {
        this.record(
            "turn.start",
            new Dictionary<string, object?> { ["turn.index"] = turn });

        return new Scope(() => this.record("turn.end", EmptyAttributes));
    }

    public void RecordTurnUsage(int promptTokens, int completionTokens, bool estimated) =>
        this.record(
            "turn.usage",
            new Dictionary<string, object?>
            {
                ["input_tokens"] = promptTokens,
                ["output_tokens"] = completionTokens,
                ["estimated"] = estimated
            });

    public void RecordRunOutcome(string status, int promptTokens, int completionTokens) =>
        this.record(
            "run.outcome",
            new Dictionary<string, object?>
            {
                ["status"] = status,
                ["input_tokens"] = promptTokens,
                ["output_tokens"] = completionTokens
            });

    private static readonly IReadOnlyDictionary<string, object?> EmptyAttributes =
        new Dictionary<string, object?>();

    private sealed class Scope(Action end) : IDisposable
    {
        public void Dispose() => end();
    }
}
