// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Brokers.Times;
using Standard.Agents.Models.Brokers.Audits;
using Standard.Agents.Models.Loggings;

namespace Standard.Agents.Brokers.Audits;

// The decision log, applied by decoration: every event the loop narrates to its log is also
// written to the audit sink as a durable record, stamped, sequenced and attributed - and the
// logging broker underneath never learns the audit exists. A decorator implements the very
// interface it takes, so it adds one concern to the call it wraps and owns no resource of its
// own; that is the one shape in which a broker may hold another (principal review 2026-09-04,
// F-16). Composed at the facade only when an audit sink is configured, over whichever logging
// broker the deployment chose - the built-in one or a host's own.
public sealed class AuditingLoggingBroker : ILoggingBroker
{
    private readonly ILoggingBroker loggingBroker;
    private readonly IAuditBroker auditBroker;
    private readonly ITimeBroker timeBroker;
    private readonly Func<string?>? principal;

    // The run is read from the ambient AgentRun that Coordination begins (SPEC.md §4.4); the
    // fallback covers a broker driven outside the loop.
    private readonly AgentRun fallbackRun = AgentRun.Detached();

    private AgentRun Run => AgentRun.Current ?? this.fallbackRun;

    public AuditingLoggingBroker(
        ILoggingBroker loggingBroker,
        IAuditBroker auditBroker,
        ITimeBroker timeBroker,
        Func<string?>? principal = null)
    {
        this.loggingBroker = loggingBroker;
        this.auditBroker = auditBroker;
        this.timeBroker = timeBroker;
        this.principal = principal;
    }

    public ValueTask LogInformationAsync(string message) =>
        this.loggingBroker.LogInformationAsync(message);

    public ValueTask LogTraceAsync(string message) =>
        this.loggingBroker.LogTraceAsync(message);

    public ValueTask LogDebugAsync(string message) =>
        this.loggingBroker.LogDebugAsync(message);

    public ValueTask LogWarningAsync(string message) =>
        this.loggingBroker.LogWarningAsync(message);

    public async ValueTask LogErrorAsync(Exception exception)
    {
        await AuditAsync(AuditKind.Error, actor: "Agent", message: exception.Message);
        await this.loggingBroker.LogErrorAsync(exception);
    }

    public async ValueTask LogCriticalAsync(Exception exception)
    {
        await AuditAsync(AuditKind.Error, actor: "Agent", message: exception.Message);
        await this.loggingBroker.LogCriticalAsync(exception);
    }

    // The trace underneath resets; the decision log is durable and records the start instead
    // (SPEC.md §4.7).
    public async ValueTask LogResetAsync()
    {
        await AuditAsync(AuditKind.Run, actor: "Agent", message: "run started");
        await this.loggingBroker.LogResetAsync();
    }

    public async ValueTask LogTurnAsync(int turn)
    {
        await AuditAsync(AuditKind.Turn, actor: "Agent", message: $"turn {turn}");
        await this.loggingBroker.LogTurnAsync(turn);
    }

    public async ValueTask LogOutcomeAsync(string message)
    {
        await AuditAsync(AuditKind.Outcome, actor: "Agent", message: message);
        await this.loggingBroker.LogOutcomeAsync(message);
    }

    public async ValueTask LogStepAsync(AgentStep step)
    {
        await AuditAsync(AuditKind.Step, actor: step.ToString(), message: step.ToString());
        await this.loggingBroker.LogStepAsync(step);
    }

    public async ValueTask LogProcessAsync(string actor, string message, bool detail = false)
    {
        await AuditAsync(AuditKind.Process, actor, message, detail);
        await this.loggingBroker.LogProcessAsync(actor, message, detail);
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
