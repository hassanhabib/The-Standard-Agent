// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Clients.Agents;
using Standard.Agents.Models.Orchestrations.Agents;

namespace Standard.Agents.Tools;

public sealed class AgentTool : ITool
{
    private const string InputPlaceholder = "{input}";
    private const string PromptPlaceholder = "{prompt}";

    /// <summary>
    /// The registry's default handoff: the task the outer brain wrote, grounded in what the
    /// user originally asked — enough context to do the task, and nothing else. A custom
    /// template replaces it wholesale, which is the whole configurability of a handoff.
    /// </summary>
    public const string GroundedHandoff = "The user asked: {prompt}\n\nYour task: {input}";

    private readonly IAgent agent;
    private readonly string handoff;

    public string Name { get; }

    public string Description { get; }

    public string Parameters { get; }

    // The three things that define the sub-agent as a tool (SPEC §6.1): the handoff (what
    // the outer agent tells it to do — a template whose "{input}" is replaced with the
    // string the outer agent supplied), a description (what it does / when to use it), and
    // parameters (a schema of its inputs). Handoff left at its default is exactly "{input}",
    // so the raw input passes through unchanged and the sub-agent behaves as before.
    public AgentTool(
        string name,
        IAgent agent,
        string handoff = InputPlaceholder,
        string description = "",
        string parameters = "{}")
    {
        this.Name = name;
        this.agent = agent;
        this.handoff = handoff;
        this.Description = description;
        this.Parameters = parameters;
    }

    // The nested agent runs its own full loop — its own Recall, Think, Act, its own
    // turn cap, its own guardians. The outer agent sees one tool call and a string
    // back, and cannot tell whether a function or a mind answered it.
    //
    // What it MUST be able to tell is whether the mind finished. A run that was held on an
    // authority, refused, or ran out of turns produced prose explaining why, and prose explaining
    // why reads exactly like prose answering the question. Returned unmarked, an outer agent can
    // report work as done that a human has not yet permitted — which is the perimeter of Part 4
    // leaking at the one seam it never covered, because every control there is scoped to a run and
    // a sub-agent is a different run.
    public async ValueTask<string> ExecuteAsync(string input)
    {
        // The outer run's token rides the ambient run the way its identity does, so the nested
        // run observes the same stop the outer loop observes — cancelling the outer run stops
        // the whole tree at the next turn boundary (SPEC.md §4.10). Outside any run, default.
        CancellationToken outerRun =
            Models.Loggings.AgentRun.Current?.CancellationToken ?? CancellationToken.None;

        // The template's placeholders resolve host-authored slots: {prompt} is what the user
        // originally asked the outer run, {input} is the task the outer model wrote. Prompt
        // first, so user text substituted into the template is never re-scanned for {input}.
        string handedOff = this.handoff
            .Replace(PromptPlaceholder, Models.Loggings.AgentRun.Current?.Prompt ?? string.Empty)
            .Replace(InputPlaceholder, input);

        AgentOutcome outcome = await this.agent.RunAsync(handedOff, outerRun);

        // How the handoff ended, recorded on the OUTER run (the nested run's own ambient state
        // never flows back across the await). The string returned below cannot carry a status,
        // and a transfer needs one: the loop adopts an answer and keeps working past anything
        // else. Recorded on every handoff — the loop clears it before each act, so it only ever
        // reads the act it just performed.
        if (Models.Loggings.AgentRun.Current is Models.Loggings.AgentRun outerAmbient)
        {
            outerAmbient.HandoffOutcome = outcome;
        }

        // An answer is an answer. Nothing is added to it, because anything added is text the outer
        // model has to reason past on the path that is working correctly.
        if (outcome.Status is AgentStatus.Responded)
        {
            return outcome.Result;
        }

        // Marked, categorised, and still carrying the sub-agent's own words. The marker is what
        // makes it unmistakable, the status is what says WHICH way it ended — held is not refused
        // and refused is not failed — and the reason is why, which a category alone cannot give.
        return $"[did not complete] the sub-agent '{Name}' ended {outcome.Status}: "
            + outcome.Result;
    }
}
