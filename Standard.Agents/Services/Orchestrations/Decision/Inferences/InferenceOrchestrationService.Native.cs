// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Brokers.Generators.V1;
using Standard.Agents.Models.Orchestrations.Agents;

namespace Standard.Agents.Services.Orchestrations.Decision.Inferences;

// Native tool calling (SPEC.md §6, "via a provider-native tool-call mechanism"). When a V1 brain
// is configured, the model's choice arrives as structured data rather than as the first line of
// its text — and the loop below is otherwise identical, because interpretation is the only part
// that differs. Everything after it — the Judge, the revision loop, Direction's perimeter — sees
// the same AgentContext it always did.
//
// Which is the point of putting the seam here: adopting native calls changes how a choice is
// read, not what the agent is.
public partial class InferenceOrchestrationService
{
    private async ValueTask<AgentContext> ThinkNativelyAsync(AgentContext context)
    {
        GenerationResult result =
            await this.brainService.GenerateAsync(context, MergedTools(context));

        await this.loggingBroker.LogProcessAsync(
            "Decision",
            result.HasToolCalls
                ? $"Brain → {result.ToolCalls.Count} native tool call(s)"
                : "Brain → answered",
            detail: true);

        // A V1 provider usually reports its own usage, and that report wins — it is what the
        // invoice is drawn from. But "usually" is not "always": a gateway or a self-hosted
        // endpoint can omit the usage object entirely, and the budget went blind whenever one
        // did. Falling through to a count keeps the bound working against every endpoint.
        AgentContext measured = await MeasuredAsync(
            context with
            {
                PromptTokens = result.PromptTokens,
                CompletionTokens = result.CompletionTokens,

                // The SAY: line's V1 twin: whichever branch below returns, the prose rides the
                // context for the loop to screen and voice.
                Narration = result.Narration
            },
            sent: context.SystemPrompt + BuildUserMessage(context),
            received: result.Content);

        if (result.HasToolCalls is false)
        {
            return measured with
            {
                Intent = RespondIntent,
                DirectionType = ReturnResponseDirection,
                Payload = result.Content,
                ToolCallId = string.Empty,
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

            // Carried so Direction can tie the result back to the call that asked for it.
            ToolCallId = call.Id,
            RawReply = result.Content
        };
    }

    // One vocabulary, two owners (design §6.1): the configured tools and the caller's, side by
    // side. The boundary already dropped any caller tool sharing a configured name, so appending
    // is safe — and only Direction knows which words are whose.
    private IReadOnlyList<ToolDefinition> MergedTools(AgentContext context)
    {
        IReadOnlyList<ToolDefinition> configured = this.toolDefinitions;

        // Selection (SPEC.md §4.15): the run is offered the selected subset of the agent's own
        // tools. Caller tools are the caller's vocabulary and are never selected away.
        if (Models.Loggings.AgentRun.Current?.OfferedTools is { } offered)
        {
            configured = [.. configured.Where(tool =>
                offered.Contains(tool.Name, StringComparer.OrdinalIgnoreCase))];
        }

        IReadOnlyList<ToolDefinition> callerTools = context.Inference?.CallerTools ?? [];

        return callerTools.Count == 0
            ? configured
            : [.. configured, .. callerTools];
    }
}
