// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Brokers.Telemetries;

/// <summary>
/// The default when no telemetry was asked for: every scope is nothing and every record goes
/// nowhere, so an agent that never called <c>.Telemetry()</c> spends nothing on it.
/// </summary>
public sealed class NotConfiguredTelemetryBroker : ITelemetryBroker
{
    public IDisposable? StartRun(string sessionId) => null;

    public IDisposable? StartTurn(int turn) => null;

    public void RecordTurnUsage(int promptTokens, int completionTokens, bool estimated)
    { }

    public void RecordRunOutcome(string status, int promptTokens, int completionTokens)
    { }
}
