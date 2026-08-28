// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Threading.Channels;
using Standard.Agents.Models.Clients.Agents;
using Standard.Agents.Models.Coordinations.Agents;
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
            // The floor: a tool that declared its narration is voiced even when the model said
            // nothing. Host-authored frame on a framework-known slot, so no gate call — the
            // only foreign content, the payload, already streamed verbatim inside Thinking.
            if (this.toolNarrations.TryGetValue(
                context.DirectionType, out ToolNarration? declared)
                    && string.IsNullOrWhiteSpace(declared.Starting) is false)
            {
                string prose = declared.Starting
                    .Replace("{tool}", context.DirectionType, StringComparison.Ordinal)
                    .Replace("{payload}", context.Payload, StringComparison.Ordinal);

                await this.loggingBroker.LogProcessAsync(
                    "Direction", $"Narration → {prose}", detail: true);

                await events.WriteAsync(
                    new AgentStreamEvent(AgentStreamEventType.Narration, prose), abandoned);
            }

            return;
        }

        string verdict = await this.decisionCoordinationService.ScreenAsync(context.Narration);

        // Withheld silently, recorded loudly: the user gains nothing from "a progress note was
        // withheld", and echoing the refusal would hand an injected SAY payload a visible
        // oracle — but the record carries the fact, which is where a review looks (SPEC.md
        // §4.7). The run itself proceeds: narration is decoration, never the work.
        if (IsRefusal(verdict))
        {
            await this.loggingBroker.LogProcessAsync(
                "Direction", $"Narration → WITHHELD: {verdict}");

            return;
        }

        await this.loggingBroker.LogProcessAsync(
            "Direction", $"Narration → {context.Narration}", detail: true);

        await events.WriteAsync(
            new AgentStreamEvent(AgentStreamEventType.Narration, context.Narration),
            abandoned);
    }

    // The observed slot: voiced after the result has been screened, immediately before the Tool
    // event that carries the data — the narration announces, the Tool event delivers. Never
    // overridden by model narration; a SAY line speaks for the act, not for its outcome.
    private async ValueTask VoiceObservedNarrationAsync(
        AgentContext context,
        ChannelWriter<AgentStreamEvent> events,
        CancellationToken abandoned)
    {
        if (this.toolNarrations.TryGetValue(context.DirectionType, out ToolNarration? declared)
            && string.IsNullOrWhiteSpace(declared.Observed) is false)
        {
            string prose = declared.Observed
                .Replace("{tool}", context.DirectionType, StringComparison.Ordinal);

            await this.loggingBroker.LogProcessAsync(
                "Direction", $"Narration → {prose}", detail: true);

            await events.WriteAsync(
                new AgentStreamEvent(AgentStreamEventType.Narration, prose), abandoned);
        }
    }
}
