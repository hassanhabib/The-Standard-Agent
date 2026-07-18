// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Text;
using Standard.Agents.Models.Clients.Agents;

namespace Standard.Agents.Services.Orchestrations.Decision;

internal sealed class ReplyStreamClassifier
{
    private const string ActionPrefix = "ACTION:";
    private const string FinalPrefix = "FINAL:";

    private readonly StringBuilder pending = new();
    private Channel channel = Channel.Undecided;

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

        bool canDecide =
            leading.Length >= ActionPrefix.Length
                || buffered.Contains('\n')
                || isFinal;

        if (canDecide is false)
        {
            return [];
        }

        this.pending.Clear();

        if (leading.StartsWith(ActionPrefix, StringComparison.OrdinalIgnoreCase))
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
