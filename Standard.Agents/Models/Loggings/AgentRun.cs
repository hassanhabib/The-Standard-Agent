// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Models.Loggings;

/// <summary>
/// One prompt's run — its identity and its counters. SPEC.md §4.4 requires run state to be
/// <b>per invocation, never per instance</b>: one agent serves many prompts at once, and an
/// implementation that keeps a run's identity or counters in shared fields lets a second prompt
/// silently corrupt the first's record.
/// </summary>
/// <remarks>
/// The run is carried as ambient per-flow state, the pattern .NET itself uses for
/// <c>Activity.Current</c> in distributed tracing. Coordination calls <see cref="Begin"/> at the
/// top of a prompt; everything that prompt awaits — every orchestration, foundation and broker
/// beneath it — reads the same run through <see cref="Current"/>, while a concurrent prompt sees
/// only its own.
///
/// <para>Why ambient rather than a parameter or a context field: the alternative is threading a
/// run identifier through <c>ILoggingBroker</c> and therefore through all thirteen services that
/// log, or adding it to <c>AgentContext</c> and making this a model change. Both cost far more
/// comprehension than they buy, and the loop and the Tri-Nature are supposed to stay the whole
/// mental model.</para>
///
/// <para>The isolation comes from the runtime: mutating an <c>AsyncLocal</c> inside an async
/// method does not flow back to its caller, so each <c>ProcessPromptAsync</c> invocation gets its
/// own run even when many are started from the same place.</para>
/// </remarks>
public sealed class AgentRun
{
    private static readonly AsyncLocal<AgentRun?> current = new();

    private int sequence;
    private int processIndex;

    private AgentRun(string id) =>
        Id = id;

    /// <summary>The run active on this flow, or <c>null</c> outside a run.</summary>
    public static AgentRun? Current => current.Value;

    /// <summary>Identifies exactly one prompt; never reused (SPEC.md §3.3).</summary>
    public string Id { get; }

    /// <summary>When the run started, stamped by the trace so the run reads one clock.</summary>
    public DateTimeOffset StartedOn { get; set; }

    /// <summary>
    /// Starts a run on the calling flow and returns the scope that ends it. Call it from the
    /// coordination loop, before anything the run should be credited with.
    /// </summary>
    public static IDisposable Begin()
    {
        AgentRun? enclosingRun = current.Value;
        current.Value = new AgentRun(Guid.NewGuid().ToString("n"));

        return new Scope(enclosingRun);
    }

    /// <summary>The next record number for this run — monotonic, starting at zero.</summary>
    public int NextSequence() =>
        Interlocked.Increment(ref this.sequence) - 1;

    /// <summary>The next process number within the current step.</summary>
    public int NextProcessIndex() =>
        Interlocked.Increment(ref this.processIndex) - 1;

    /// <summary>Restarts process numbering, called when a step begins.</summary>
    public void ResetProcessIndex() =>
        Interlocked.Exchange(ref this.processIndex, 0);

    // Restores whatever run was active before, so a nested agent (the fractal — an agent used as
    // a tool of another agent) returns its caller's run rather than leaving it cleared.
    private sealed class Scope : IDisposable
    {
        private readonly AgentRun? enclosingRun;

        internal Scope(AgentRun? enclosingRun) =>
            this.enclosingRun = enclosingRun;

        public void Dispose() =>
            current.Value = this.enclosingRun;
    }
}
