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

    ValueTask LogStepAsync(AgentStep step);

    ValueTask LogProcessAsync(string actor, string message, bool detail = false);
}
