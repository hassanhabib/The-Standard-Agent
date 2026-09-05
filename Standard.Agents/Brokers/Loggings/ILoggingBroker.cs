// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Loggings;

namespace Standard.Agents.Brokers.Loggings;

public interface ILoggingBroker
{
    ValueTask LogInformationAsync(string message);

    ValueTask LogTraceAsync(string message);

    ValueTask LogDebugAsync(string message);

    ValueTask LogWarningAsync(string message);

    ValueTask LogErrorAsync(Exception exception);

    ValueTask LogCriticalAsync(Exception exception);

    ValueTask LogResetAsync();

    ValueTask LogTurnAsync(int turn);

    ValueTask LogOutcomeAsync(string message);

    ValueTask LogStepAsync(AgentStep step);

    ValueTask LogProcessAsync(string actor, string message, bool detail = false);

    /// <summary>
    /// A process event that carries a payload: the prompt, the system prompt, the Brain's reply,
    /// a tool's input or output. The summary says what happened; the payload is the content, kept
    /// apart so a sink can decide what it keeps (principal review 2026-09-04, F-14). A broker that
    /// does not distinguish the two logs them as one message.
    /// </summary>
    ValueTask LogPayloadAsync(string actor, string summary, string payload, bool detail) =>
        LogProcessAsync(actor, $"{summary} → {payload}", detail);
}
