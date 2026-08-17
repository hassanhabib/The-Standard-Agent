// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Brokers.Generators.V1;
using Standard.Agents.Models.Orchestrations.Agents;

namespace Standard.Agents.Services.Orchestrations.Decision;

// Native tool calling (SPEC.md §6, "via a provider-native tool-call mechanism"). When a V1 brain
// is configured, the model's choice arrives as structured data rather than as the first line of
// its text — and the loop below is otherwise identical, because interpretation is the only part
// that differs. Everything after it — the Judge, the revision loop, Direction's perimeter — sees
// the same AgentContext it always did.
//
// Which is the point of putting the seam here: adopting native calls changes how a choice is
// read, not what the agent is.
public partial class DecisionOrchestrationService
{
    private async ValueTask<AgentContext> ThinkNativelyAsync(AgentContext context)
    {
        GenerationResult result =
            await this.brainService.GenerateAsync(context, this.toolDefinitions);

        await this.loggingBroker.LogProcessAsync(
            "Decision",
            result.HasToolCalls
                ? $"Brain → {result.ToolCalls.Count} native tool call(s)"
                : "Brain → answered",
            detail: true);

        AgentContext measured = context with
        {
            PromptTokens = result.PromptTokens,
            CompletionTokens = result.CompletionTokens
        };

        if (result.HasToolCalls is false)
        {
            return measured with
            {
                Intent = RespondIntent,
                DirectionType = ReturnResponseDirection,
                Payload = result.Content,
                RawReply = result.Content
            };
        }

        // One call per turn, deliberately. A model may ask for several; Direction performs one
        // act at a time because authorization, approval and run-once are judgments about a
        // single act (SPEC.md §4.9). The rest are re-proposed on the next turn if still wanted,
        // and run-once means a repeat of one already performed costs nothing.
        ModelToolCall call = result.ToolCalls[0];

        await this.loggingBroker.LogProcessAsync(
            "Decision", $"Interpreted → {call.Name} (native)");

        return measured with
        {
            Intent = call.Name,
            DirectionType = call.Name,
            Payload = call.ArgumentsJson,
            RawReply = result.Content
        };
    }
}
