// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Text;
using Standard.Agents.Models.Clients.Agents;

namespace Standard.Agents.Services.Orchestrations.Decision.Inferences;

internal sealed class ReplyStreamClassifier
{
    private const string ActionPrefix = "ACTION:";
    private const string FinalPrefix = "FINAL:";
    private const string TransferPrefix = "TRANSFER:";
    private const string SayPrefix = "SAY:";

    private readonly StringBuilder pending = new();
    private Channel channel = Channel.Undecided;
    private bool narrationSwallowed;

    private enum Channel
    {
        Undecided,
        Thinking,
        Responding
    }

    public IEnumerable<AgentStreamEvent> Classify(string delta)
    {
        if (this.channel is Channel.Thinking)
        {
            return [new AgentStreamEvent(AgentStreamEventType.Thinking, delta)];
        }

        if (this.channel is Channel.Responding)
        {
            return [new AgentStreamEvent(AgentStreamEventType.Response, delta)];
        }

        this.pending.Append(delta);

        return Decide(isFinal: false);
    }

    public IEnumerable<AgentStreamEvent> Flush() =>
        this.channel is Channel.Undecided
            ? Decide(isFinal: true)
            : [];

    private IEnumerable<AgentStreamEvent> Decide(bool isFinal)
    {
        string buffered = this.pending.ToString();
        string leading = buffered.TrimStart();

        // A leading SAY: line is swallowed whole, never streamed: it is narration, and narration
        // is screened by the loop before it is voiced — a delta streamed here would cross that
        // boundary unvetted. Checked before the length-based commit below, because "SAY: Let m"
        // reaches nine characters and would otherwise commit as an answer starting to stream.
        // Interpret peels the same line at end of turn, so the prose still rides the context.
        if (this.narrationSwallowed is false
            && leading.StartsWith(SayPrefix, StringComparison.OrdinalIgnoreCase))
        {
            // The newline that ends the SAY line, not one hiding in the leading whitespace.
            int newlineIndex = buffered.IndexOf('\n', buffered.Length - leading.Length);

            if (newlineIndex < 0)
            {
                if (isFinal)
                {
                    this.pending.Clear();
                }

                return [];
            }

            this.pending.Clear();
            this.pending.Append(buffered[(newlineIndex + 1)..]);
            this.narrationSwallowed = true;

            return Decide(isFinal);
        }

        // Buffered until the LONGEST act prefix could be ruled out: deciding at ACTION's length
        // would read the first seven characters of "TRANSFER:" as an answer starting to stream.
        bool canDecide =
            leading.Length >= TransferPrefix.Length
                || buffered.Contains('\n')
                || isFinal;

        if (canDecide is false)
        {
            return [];
        }

        this.pending.Clear();

        if (leading.StartsWith(ActionPrefix, StringComparison.OrdinalIgnoreCase)
            || leading.StartsWith(TransferPrefix, StringComparison.OrdinalIgnoreCase))
        {
            this.channel = Channel.Thinking;

            return [new AgentStreamEvent(AgentStreamEventType.Thinking, buffered)];
        }

        this.channel = Channel.Responding;

        int finalIndex = buffered.IndexOf(FinalPrefix, StringComparison.OrdinalIgnoreCase);

        if (finalIndex < 0)
        {
            return [new AgentStreamEvent(AgentStreamEventType.Response, buffered)];
        }

        string head = buffered[..(finalIndex + FinalPrefix.Length)];
        string answer = buffered[(finalIndex + FinalPrefix.Length)..].TrimStart();

        List<AgentStreamEvent> events = [new AgentStreamEvent(AgentStreamEventType.Thinking, head)];

        if (answer.Length > 0)
        {
            events.Add(new AgentStreamEvent(AgentStreamEventType.Response, answer));
        }

        return events;
    }
}
