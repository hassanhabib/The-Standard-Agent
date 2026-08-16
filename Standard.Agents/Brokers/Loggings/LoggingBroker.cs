// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Standard.Agents.Brokers.Audits;
using Standard.Agents.Brokers.Times;
using Standard.Agents.Models.Brokers.Audits;
using Standard.Agents.Models.Loggings;

namespace Standard.Agents.Brokers.Loggings;

public sealed class LoggingBroker : ILoggingBroker
{
    private readonly ILogger<LoggingBroker> logger;
    private readonly ITimeBroker timeBroker;
    private readonly IAuditBroker auditBroker;
    private readonly TraceVerbosity verbosity;
    private readonly string? logPath;
    private readonly Func<string?>? principal;

    // No run state lives here. SPEC.md §4.4: run identity and counters are per invocation,
    // never per instance — one broker serves every concurrent run, so a field here would let
    // one prompt overwrite another's record. The run is read from the ambient AgentRun that
    // Coordination begins; the fallback run covers a broker driven outside the loop.
    private readonly AgentRun fallbackRun = AgentRun.Detached();
    private readonly SemaphoreSlim traceLock = new(initialCount: 1, maxCount: 1);

    private AgentRun Run => AgentRun.Current ?? this.fallbackRun;

    public LoggingBroker(
        ILogger<LoggingBroker> logger,
        ITimeBroker timeBroker,
        TraceVerbosity verbosity = TraceVerbosity.Full,
        string? logPath = null,
        IAuditBroker? auditBroker = null,
        Func<string?>? principal = null)
    {
        this.logger = logger;
        this.timeBroker = timeBroker;
        this.auditBroker = auditBroker ?? new NotConfiguredAuditBroker();
        this.verbosity = verbosity;
        this.logPath = logPath is null ? null : Path.GetFullPath(logPath);
        this.principal = principal;
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

        await AuditAsync(AuditKind.Error, actor: "Agent", message: exception.Message);
        await EmitAsync($"  → ERROR: {exception.Message.ReplaceLineEndings(" ")}");
    }

    public async ValueTask LogCriticalAsync(Exception exception)
    {
        this.logger.LogCritical(exception, exception.Message);

        await AuditAsync(AuditKind.Error, actor: "Agent", message: exception.Message);
        await EmitAsync($"  → CRITICAL: {exception.Message.ReplaceLineEndings(" ")}");
    }

    // Starts a run. The human-readable trace is transient and is reset here; the decision
    // log is durable and MUST NOT be — SPEC.md §4.7 is explicit that this reset does not
    // propagate to it. Truncating the audit here is exactly the defect this release fixes.
    public async ValueTask LogResetAsync()
    {
        this.Run.ResetProcessIndex();
        this.Run.StartedOn = this.timeBroker.GetCurrentDateTimeOffset();

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

        await AuditAsync(AuditKind.Run, actor: "Agent", message: "run started");
    }

    public async ValueTask LogTurnAsync(int turn)
    {
        await AuditAsync(AuditKind.Turn, actor: "Agent", message: $"turn {turn}");

        await EmitAsync($"{Environment.NewLine}Turn {turn}");
    }

    public async ValueTask LogOutcomeAsync(string message)
    {
        TimeSpan elapsed = this.timeBroker.GetCurrentDateTimeOffset() - this.Run.StartedOn;

        await AuditAsync(AuditKind.Outcome, actor: "Agent", message: message);

        await EmitAsync($"  → {message} ({elapsed.TotalMilliseconds:F0}ms)");
    }

    public async ValueTask LogStepAsync(AgentStep step)
    {
        this.Run.ResetProcessIndex();

        await AuditAsync(AuditKind.Step, actor: step.ToString(), message: step.ToString());

        if (TraceVerbosity.Natures <= this.verbosity)
        {
            await EmitAsync($"  Step {(int)step}: {step}");
        }
    }

    public async ValueTask LogProcessAsync(string actor, string message, bool detail = false)
    {
        await AuditAsync(AuditKind.Process, actor, message, detail);

        TraceVerbosity level = detail ? TraceVerbosity.Full : TraceVerbosity.Natures;

        if (level <= this.verbosity)
        {
            string indented = message.ReplaceLineEndings($"{Environment.NewLine}      ");

            await EmitAsync($"    Process {this.Run.NextProcessIndex()}: {actor}: {indented}");
        }
    }

    // The trace is a shared file and one broker serves every concurrent run, so writes are
    // serialized — without this, sixty-four prompts in flight collide on the handle and most
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

    private async ValueTask AuditAsync(
        AuditKind kind,
        string actor,
        string message,
        bool detail = false)
    {
        AgentRun run = this.Run;

        var record = new AuditRecord
        {
            RunId = run.Id,
            Sequence = run.NextSequence(),
            Timestamp = this.timeBroker.GetCurrentDateTimeOffset(),
            Kind = kind,
            Actor = actor,
            Message = message,
            Detail = detail,
            Principal = this.principal?.Invoke()
        };

        await this.auditBroker.WriteAsync(record);
    }
}
