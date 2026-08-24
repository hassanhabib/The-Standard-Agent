// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Brokers.Telemetries;

/// <summary>
/// The telemetry seam — spans and metrics for the run the way <c>ILoggingBroker</c> is prose and
/// <c>IAuditBroker</c> is records. A utility broker: held by the loop, exempt from the dependency
/// count, and structurally unable to change what the agent decides or does.
/// </summary>
public interface ITelemetryBroker
{
    /// <summary>
    /// Opens the run scope. Dispose it to close the scope; a <c>null</c> return means nothing is
    /// listening and there is no scope to close.
    /// </summary>
    IDisposable? StartRun(string sessionId);

    /// <summary>Opens one turn's scope inside the run scope.</summary>
    IDisposable? StartTurn(int turn);

    /// <summary>Records what one turn's model calls consumed, and whether it was counted or reported.</summary>
    void RecordTurnUsage(int promptTokens, int completionTokens, bool estimated);

    /// <summary>Records how the run ended and what it consumed in total.</summary>
    void RecordRunOutcome(string status, int promptTokens, int completionTokens);
}
