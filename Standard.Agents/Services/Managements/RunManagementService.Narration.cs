// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Threading.Channels;
using Standard.Agents.Models.Clients.Agents;
using Standard.Agents.Models.Orchestrations.Agents;

namespace Standard.Agents.Services.Managements;

// Narration: the agent says what it is doing, in the user's language, on a typed channel of its
// own (SPEC.md §6.0). Voiced here and nowhere else — the loop is the single copy both doors
// share (SPEC.md §7.6), so the gate calls and log lines below are identical between a streamed
// and a batched run by construction; the batched door simply discards the event.
public partial class RunManagementService
{
    // Model-authored narration is model output crossing to the user with no Judge and no
    // Contract between them — the Gate is its only guardian, so it screens unconditionally
    // rather than behind the tool-output flag, which guards model INPUT (SPEC.md §4.9).
    private async ValueTask VoiceNarrationAsync(
        AgentContext context,
        ChannelWriter<AgentStreamEvent> events,
        CancellationToken abandoned)
    {
        if (string.IsNullOrWhiteSpace(context.Narration))
        {
            return;
        }

        await this.decisionCoordinationService.ScreenAsync(context.Narration);

        await this.loggingBroker.LogProcessAsync(
            "Direction", $"Narration → {context.Narration}", detail: true);

        await events.WriteAsync(
            new AgentStreamEvent(AgentStreamEventType.Narration, context.Narration),
            abandoned);
    }
}
