// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Brokers.Generators.V1;
using Standard.Agents.Models.Brokers.Sessions;
using Standard.Agents.Models.Orchestrations.Agents;

namespace Standard.Agents.Services.Foundations.Brains;

// The V1 path: a conversation of typed messages in, a structured choice out.
//
// The whole reason it exists is that the text protocol forfeits what hosted models are trained
// on. A model asked to emit "ACTION: calculator: 2+2" is being asked to imitate a format; the
// same model emitting a tool_call is doing the thing it was tuned for. The text protocol stays
// the Core contract because it works against any endpoint, and because a small local model
// often does better with it.
//
// Redaction applies here exactly as it does on the V0 path (SPEC.md §4.6): every message going
// out is redacted, and the reply is rehydrated before anyone reads it.
public partial class BrainService
{
    public async ValueTask<GenerationResult> GenerateAsync(
        AgentContext context,
        IReadOnlyList<ToolDefinition> tools)
    {
        ValidateUserPrompt(context.Prompt);

        IReadOnlyList<ConversationMessage> messages = [.. BuildConversation(context)];

        // The context arrived intact; it used to be flattened here and its resolution discarded
        // (docs/per-request-inference.md §2). Now the resolved options ride the last hop too —
        // the broker honors them or degrades by its own default member, and either way the
        // guardian still holds the answer to shape.
        return context.Inference is null
            ? await this.generatorBrokerV1!.GenerateAsync(messages, tools)
            : await this.generatorBrokerV1!.GenerateAsync(messages, tools, context.Inference);
    }

    // The conversation the model actually sees: who it is, what was said before, what it has
    // learned this turn, and the task. Observations are their own message rather than appended
    // to the task, so a tool result reads as something that happened rather than as part of
    // what the user asked.
    private static IEnumerable<ConversationMessage> BuildConversation(AgentContext context)
    {
        if (string.IsNullOrWhiteSpace(context.SystemPrompt) is false)
        {
            yield return new ConversationMessage
            {
                Role = MessageRole.System,
                Content = context.SystemPrompt
            };
        }

        foreach (AgentTurn turn in context.History)
        {
            yield return new ConversationMessage
            {
                Role = MessageRole.User,
                Content = turn.Prompt
            };

            yield return new ConversationMessage
            {
                Role = MessageRole.Assistant,
                Content = turn.Answer
            };
        }

        yield return new ConversationMessage
        {
            Role = MessageRole.User,
            Content = context.Prompt
        };

        // Native calls go back as what they were: the assistant's request, then the tool's answer
        // naming the call it answers (SPEC.md §6). This is the shape hosted models are trained on,
        // and it is the reason the id exists — narrating "calculator: 4183" would leave the model
        // matching answers to questions by reading.
        foreach (ToolExchange exchange in context.ToolExchanges)
        {
            yield return new ConversationMessage
            {
                Role = MessageRole.Assistant,

                ToolCalls =
                [
                    new ModelToolCall(
                        exchange.CallId, exchange.ToolName, exchange.ArgumentsJson)
                ]
            };

            yield return new ConversationMessage
            {
                Role = MessageRole.Tool,
                ToolCallId = exchange.CallId,
                Content = exchange.Result
            };
        }

        // What is left is everything the exchanges do not already carry — a denial, a screening
        // refusal, a V0-shaped observation. Dropping these would lose the very results the
        // perimeter withheld, which are the ones the model most needs to see.
        string[] alreadySent =
            [.. context.ToolExchanges.Select(exchange =>
                $"{exchange.ToolName}: {exchange.Result}")];

        string[] narrated =
            [.. context.Observations.Where(observation =>
                alreadySent.Contains(observation, StringComparer.Ordinal) is false)];

        if (narrated.Length > 0)
        {
            yield return new ConversationMessage
            {
                Role = MessageRole.Assistant,
                Content = "Observations so far:\n"
                    + string.Join('\n', narrated.Select(o => $"- {o}"))
            };
        }
    }
}
