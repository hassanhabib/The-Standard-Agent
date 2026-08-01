// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Standard.Agents.Models.Loggings;

namespace Standard.Agents.Brokers.Loggings;

public sealed class LoggingBroker : ILoggingBroker
{
    private readonly ILogger<LoggingBroker> logger;
    private readonly TraceVerbosity verbosity;
    private int processIndex;

    public LoggingBroker(
        ILogger<LoggingBroker> logger,
        TraceVerbosity verbosity = TraceVerbosity.Full)
    {
        this.logger = logger;
        this.verbosity = verbosity;
    }

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

    public async ValueTask LogTurnAsync(int turn) =>
        this.logger.LogInformation($"{Environment.NewLine}Turn {turn}");

    public async ValueTask LogStepAsync(AgentStep step)
    {
        this.processIndex = 0;

        if (TraceVerbosity.Natures <= this.verbosity)
        {
            this.logger.LogInformation($"  Step {(int)step}: {step}");
        }
    }

    public async ValueTask LogProcessAsync(string actor, string message, bool detail = false)
    {
        TraceVerbosity level = detail ? TraceVerbosity.Full : TraceVerbosity.Natures;

        if (level <= this.verbosity)
        {
            this.logger.LogInformation($"    Process {this.processIndex++}: {actor}: {message}");
        }
    }
}
