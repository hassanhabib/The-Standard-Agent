// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Standard.Agents.Models.Loggings;

namespace Standard.Agents.Brokers.Loggings;

// One resource, wrapped: the log. Both of its faces are the same sink - the ILogger the host
// gave us, and the human-readable trace file that is that log written to disk. Nothing else
// lives here. The decision log is another resource with another broker, and the two meet only
// by decoration at composition (AuditingLoggingBroker), never by this broker holding that one
// (principal review 2026-09-04, F-16).
public sealed class LoggingBroker : ILoggingBroker
{
    private readonly ILogger<LoggingBroker> logger;
    private readonly TraceVerbosity verbosity;
    private readonly string? logPath;

    // No run state lives here. SPEC.md §4.4: run identity and counters are per invocation,
    // never per instance - one broker serves every concurrent run, so a field here would let
    // one prompt overwrite another's record. The run is read from the ambient AgentRun that
    // Coordination begins; the fallback run covers a broker driven outside the loop.
    private readonly AgentRun fallbackRun = AgentRun.Detached();
    private readonly SemaphoreSlim traceLock = new(initialCount: 1, maxCount: 1);

    private AgentRun Run => AgentRun.Current ?? this.fallbackRun;

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

    // Starts a run. The human-readable trace is transient and is reset here; the decision log
    // is durable and is not touched by this broker at all (SPEC.md §4.7).
    public async ValueTask LogResetAsync()
    {
        this.Run.ResetProcessIndex();
        this.Run.StartedOn = DateTimeOffset.UtcNow;

        if (this.logPath is not null)
        {
            await this.traceLock.WaitAsync();

            try
            {
                await File.WriteAllTextAsync(this.logPath, string.Empty);
            }
            finally
            {
                this.traceLock.Release();
            }
        }
    }

    public async ValueTask LogTurnAsync(int turn) =>
        await EmitAsync($"{Environment.NewLine}Turn {turn}");

    public async ValueTask LogOutcomeAsync(string message)
    {
        TimeSpan elapsed = DateTimeOffset.UtcNow - this.Run.StartedOn;
        await EmitAsync($"  → {message} ({elapsed.TotalMilliseconds:F0}ms)");
    }

    public async ValueTask LogStepAsync(AgentStep step)
    {
        this.Run.ResetProcessIndex();

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
            await EmitAsync($"    Process {this.Run.NextProcessIndex()}: {actor}: {indented}");
        }
    }

    // The trace is a shared file and one broker serves every concurrent run, so writes are
    // serialized - without this, sixty-four prompts in flight collide on the handle and most
    // of them fail with an IOException rather than an answer.
    //
    // Concurrent runs interleave in the trace, and each run's reset truncates it. That is the
    // trace being what SPEC.md §4.7 calls it: transient, a development aid, resettable. The
    // artifact for a concurrent deployment is the decision log, which is append-only and keeps
    // every run whole.
    private async ValueTask EmitAsync(string line)
    {
        if (this.logPath is null)
        {
            this.logger.LogInformation(line);

            return;
        }

        await this.traceLock.WaitAsync();

        try
        {
            await File.AppendAllTextAsync(this.logPath, line + Environment.NewLine);
        }
        finally
        {
            this.traceLock.Release();
        }
    }
}
