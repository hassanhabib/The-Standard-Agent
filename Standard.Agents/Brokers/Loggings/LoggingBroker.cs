// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Standard.Agents.Brokers.Times;
using Standard.Agents.Models.Loggings;

namespace Standard.Agents.Brokers.Loggings;

public sealed class LoggingBroker : ILoggingBroker
{
    private readonly ILogger<LoggingBroker> logger;
    private readonly ITimeBroker timeBroker;
    private readonly TraceVerbosity verbosity;
    private readonly string? logPath;
    private readonly string? auditPath;
    private int processIndex;
    private DateTimeOffset runStart;

    public LoggingBroker(
        ILogger<LoggingBroker> logger,
        ITimeBroker timeBroker,
        TraceVerbosity verbosity = TraceVerbosity.Full,
        string? logPath = null,
        string? auditPath = null)
    {
        this.logger = logger;
        this.timeBroker = timeBroker;
        this.verbosity = verbosity;
        this.logPath = logPath is null ? null : Path.GetFullPath(logPath);
        this.auditPath = auditPath is null ? null : Path.GetFullPath(auditPath);
    }

    public async ValueTask LogInformationAsync(string message) =>
        this.logger.LogInformation(message);

    public async ValueTask LogTraceAsync(string message) =>
        this.logger.LogTrace(message);

    public async ValueTask LogDebugAsync(string message) =>
        this.logger.LogDebug(message);

    public async ValueTask LogWarningAsync(string message) =>
        this.logger.LogWarning(message);

    public async ValueTask LogErrorAsync(Exception exception)
    {
        this.logger.LogError(exception, exception.Message);

        await EmitAsync($"  → ERROR: {exception.Message.ReplaceLineEndings(" ")}");
    }

    public async ValueTask LogCriticalAsync(Exception exception)
    {
        this.logger.LogCritical(exception, exception.Message);

        await EmitAsync($"  → CRITICAL: {exception.Message.ReplaceLineEndings(" ")}");
    }

    public async ValueTask LogResetAsync()
    {
        this.processIndex = 0;
        this.runStart = this.timeBroker.GetCurrentDateTimeOffset();

        if (this.logPath is not null)
        {
            await File.WriteAllTextAsync(this.logPath, string.Empty);
        }
    }

    public async ValueTask LogTurnAsync(int turn) =>
        await EmitAsync($"{Environment.NewLine}Turn {turn}");

    public async ValueTask LogOutcomeAsync(string message)
    {
        TimeSpan elapsed = this.timeBroker.GetCurrentDateTimeOffset() - this.runStart;

        await EmitAsync($"  → {message} ({elapsed.TotalMilliseconds:F0}ms)");
    }

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
            string indented = message.ReplaceLineEndings($"{Environment.NewLine}      ");

            await EmitAsync($"    Process {this.processIndex++}: {actor}: {indented}");
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
