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
    private readonly string? logPath;
    private int processIndex;

    public LoggingBroker(
        ILogger<LoggingBroker> logger,
        TraceVerbosity verbosity = TraceVerbosity.Full,
        string? logPath = null)
    {
        this.logger = logger;
        this.verbosity = verbosity;
        this.logPath = logPath is null ? null : Path.GetFullPath(logPath);
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

    public async ValueTask LogResetAsync()
    {
        this.processIndex = 0;

        if (this.logPath is not null)
        {
            await File.WriteAllTextAsync(this.logPath, string.Empty);
        }
    }

    public async ValueTask LogTurnAsync(int turn) =>
        await EmitAsync($"{Environment.NewLine}Turn {turn}");

    public async ValueTask LogStepAsync(AgentStep step)
    {
        this.processIndex = 0;

        if (TraceVerbosity.Natures <= this.verbosity)
        {
            await EmitAsync($"  Step {(int)step}: {step}");
        }
    }

    public async ValueTask LogProcessAsync(string actor, string message, bool detail = false)
    {
        TraceVerbosity level = detail ? TraceVerbosity.Full : TraceVerbosity.Natures;

        if (level <= this.verbosity)
        {
            await EmitAsync($"    Process {this.processIndex++}: {actor}: {message}");
        }
    }

    private async ValueTask EmitAsync(string line)
    {
        if (this.logPath is null)
        {
            this.logger.LogInformation(line);
        }
        else
        {
            await File.AppendAllTextAsync(this.logPath, line + Environment.NewLine);
        }
    }
}
