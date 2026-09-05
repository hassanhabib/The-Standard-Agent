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

    private readonly Dictionary<string, string> verdicts = [];
    private readonly HashSet<string> grants = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<PerformedEffect> performedEffects = [];

    private int sequence;
    private int processIndex;

    private AgentRun(string id) =>
        Id = id;

    /// <summary>The run active on this flow, or <c>null</c> outside a run.</summary>
    public static AgentRun? Current => current.Value;

    /// <summary>Identifies exactly one prompt; never reused (SPEC.md §3.3).</summary>
    public string Id { get; }

    /// <summary>
    /// The token that stops this run, riding the run the way its identity does — so anything
    /// the run reaches, a nested agent included, can observe the same stop the outer loop
    /// observes at its turn boundaries (SPEC.md §4.10). Default when the caller supplied none.
    /// </summary>
    public CancellationToken CancellationToken { get; private set; }

    /// <summary>When the run started, stamped by the trace so the run reads one clock.</summary>
    public DateTimeOffset StartedOn { get; set; }

    /// <summary>
    /// What the user originally asked this run, riding the run the way its token does — so a
    /// handoff can ground a sub-agent in the real goal (<c>{prompt}</c> in a handoff template)
    /// without the outer model having to restate it. Empty outside a prompt.
    /// </summary>
    public string Prompt { get; set; } = "";

    /// <summary>
    /// How the most recent handoff this run performed ended — recorded by <c>AgentTool</c> when
    /// the nested agent returns, cleared by the loop before every act. The tool seam is
    /// string-typed and cannot carry a status, and a transfer needs one: an answer is adopted
    /// as the run's answer, and any other ending stays an observation.
    /// </summary>
    public Clients.Agents.AgentOutcome? HandoffOutcome { get; set; }

    /// <summary>
    /// What this run is OFFERED (SPEC.md §4.15): the selected subset of the agent's described
    /// tools, set by the loop once per run when a selector is configured, and read wherever the
    /// offering is rendered — the text catalog and the native tool list alike. <c>null</c>
    /// means no selection ran: every described tool is offered, the unamended behavior. Riding
    /// the run for the same reason the prompt does: two tiers below the loop read it, and
    /// neither should gain a parameter for it.
    /// </summary>
    public IReadOnlyList<string>? OfferedTools { get; set; }

    /// <summary>
    /// The remote tools this run discovered on its servers, set by the loop once per run before
    /// selection and read wherever the offering is rendered or bound: the text catalog, the
    /// native tool list, and the perimeter's notion of what is advertised. Empty when there are
    /// none. Riding the run for the same reason the offering does — the readers sit two tiers
    /// below the loop, and none should gain a parameter for it.
    /// </summary>
    public IReadOnlyList<Brokers.Mcps.McpTool> RemoteTools { get; set; } = [];

    /// <summary>
    /// Starts a run on the calling flow and returns the scope that ends it. Call it from the
    /// coordination loop, before anything the run should be credited with.
    /// </summary>
    /// <param name="resumedId">
    /// The identity of a run this one continues, or <c>null</c> to start a new one. A resumed run
    /// keeps the interrupted run's identity so the effects it already performed are recognised as
    /// its own rather than proposed again (SPEC.md §4.9, §4.11).
    /// </param>
    /// <param name="cancellationToken">
    /// The token that stops this run, carried on the run so a nested agent can observe the
    /// same stop the outer loop observes.
    /// </param>
    public static IDisposable Begin(
        string? resumedId = null,
        CancellationToken cancellationToken = default)
    {
        AgentRun? enclosingRun = current.Value;

        current.Value = new AgentRun(
            string.IsNullOrEmpty(resumedId)
                ? Guid.NewGuid().ToString("n")
                : resumedId)
        {
            CancellationToken = cancellationToken
        };

        return new Scope(enclosingRun);
    }

    /// <summary>
    /// A run that is not on any flow — for a trace driven outside the coordination loop, so it
    /// still numbers its records rather than faulting.
    /// </summary>
    public static AgentRun Detached() =>
        new(Guid.NewGuid().ToString("n"));

    /// <summary>The next record number for this run — monotonic, starting at zero.</summary>
    public int NextSequence() =>
        Interlocked.Increment(ref this.sequence) - 1;

    /// <summary>The next process number within the current step.</summary>
    public int NextProcessIndex() =>
        Interlocked.Increment(ref this.processIndex) - 1;

    /// <summary>
    /// Remembers a guardian verdict for the life of this run, so an unchanged prompt is screened
    /// once rather than once per turn (SPEC.md §4.10). It lives on the run rather than in a
    /// service-level cache so it is evicted by the run ending, with nothing to remember to clear.
    /// </summary>
    public bool TryGetVerdict(string prompt, out string verdict)
    {
        lock (this.verdicts)
        {
            return this.verdicts.TryGetValue(prompt, out verdict!);
        }
    }

    public void RememberVerdict(string prompt, string verdict)
    {
        lock (this.verdicts)
        {
            this.verdicts[prompt] = verdict;
        }
    }

    /// <summary>
    /// Whether an authority has already permitted this tool at this scope during this run.
    /// </summary>
    /// <remarks>
    /// A grant is for <b>what it was granted for</b>. Approving a write to one file is not
    /// approving writes to every file, so the key is the tool AND the scope and the match is
    /// exact — a broader grant ("anything under /project") is a judgement only the authority can
    /// make, and an approval broker that wants to make it can say so by answering the next
    /// request without asking anyone.
    /// <para>It lives on the run for the same reason the guardian verdicts do: the run ending
    /// evicts it, and there is nothing to remember to clear.</para>
    /// </remarks>
    public bool WasGranted(string toolName, string scope)
    {
        lock (this.grants)
        {
            return this.grants.Contains(GrantKey(toolName, scope));
        }
    }

    /// <summary>Remembers that an authority permitted this tool at this scope.</summary>
    public void RememberGrant(string toolName, string scope)
    {
        lock (this.grants)
        {
            this.grants.Add(GrantKey(toolName, scope));
        }
    }

    private static string GrantKey(string toolName, string scope) =>
        $"{toolName} {scope}";

    /// <summary>
    /// What this run actually performed, in order. Kept so the run can be unwound: compensation
    /// needs to know what happened, and only the run knows (SPEC.md §4.9).
    /// </summary>
    public IReadOnlyList<PerformedEffect> PerformedEffects
    {
        get
        {
            lock (this.performedEffects)
            {
                return [.. this.performedEffects];
            }
        }
    }

    public void RecordPerformed(PerformedEffect effect)
    {
        lock (this.performedEffects)
        {
            this.performedEffects.Add(effect);
        }
    }

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
