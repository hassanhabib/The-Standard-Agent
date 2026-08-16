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

    private int processIndex;
    private int sequence;
    private string runId = string.Empty;
    private DateTimeOffset runStart;

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
        this.processIndex = 0;
        this.sequence = 0;
        this.runId = Guid.NewGuid().ToString("n");
        this.runStart = this.timeBroker.GetCurrentDateTimeOffset();

        if (this.logPath is not null)
        {
            await File.WriteAllTextAsync(this.logPath, string.Empty);
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
        TimeSpan elapsed = this.timeBroker.GetCurrentDateTimeOffset() - this.runStart;

        await AuditAsync(AuditKind.Outcome, actor: "Agent", message: message);

        await EmitAsync($"  → {message} ({elapsed.TotalMilliseconds:F0}ms)");
    }

    public async ValueTask LogStepAsync(AgentStep step)
    {
        this.processIndex = 0;

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

    private async ValueTask AuditAsync(
        AuditKind kind,
        string actor,
        string message,
        bool detail = false)
    {
        var record = new AuditRecord
        {
            RunId = this.runId,
            Sequence = this.sequence++,
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
