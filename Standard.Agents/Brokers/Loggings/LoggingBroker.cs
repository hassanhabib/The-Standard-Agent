// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Standard.Agents.Brokers.Loggings;

public sealed class LoggingBroker : ILoggingBroker
{
    private readonly ILogger<LoggingBroker> logger;

    public LoggingBroker(ILogger<LoggingBroker> logger) =>
        this.logger = logger;

    // ILogger<T> is synchronous. These are async ValueTask for the uniform contract
    // (The Standard 1.5.1) and call straight through — never Task.Run.
    public async ValueTask LogInformationAsync(string message) =>
        this.logger.LogInformation(message);

    public async ValueTask LogTraceAsync(string message) =>
        this.logger.LogTrace(message);

    public async ValueTask LogDebugAsync(string message) =>
        this.logger.LogDebug(message);

    public async ValueTask LogWarningAsync(string message) =>
        this.logger.LogWarning(message);

    public async ValueTask LogErrorAsync(Exception exception) =>
        this.logger.LogError(exception, exception.Message);

    public async ValueTask LogCriticalAsync(Exception exception) =>
        this.logger.LogCritical(exception, exception.Message);
}
