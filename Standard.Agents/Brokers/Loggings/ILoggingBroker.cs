// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Brokers.Loggings;

public interface ILoggingBroker
{
    ValueTask LogInformationAsync(string message);

    ValueTask LogTraceAsync(string message);

    ValueTask LogDebugAsync(string message);

    ValueTask LogWarningAsync(string message);

    ValueTask LogErrorAsync(Exception exception);

    ValueTask LogCriticalAsync(Exception exception);
}
