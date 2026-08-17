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

        var vault = new Dictionary<string, string>();

        IReadOnlyList<ConversationMessage> messages =
            [.. BuildConversation(context).Select(message => message with
            {
                Content = this.redactionBroker.Redact(message.Content, vault)
            })];

        GenerationResult result = await this.resilienceBroker.ExecuteAsync(() =>
            this.generatorBrokerV1!.GenerateAsync(messages, tools));

        return result with
        {
            Content = this.redactionBroker.Rehydrate(result.Content, vault),

            ToolCalls =
            [
                .. result.ToolCalls.Select(call => call with
                {
                    ArgumentsJson = this.redactionBroker.Rehydrate(call.ArgumentsJson, vault)
                })
            ]
        };
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

        if (context.Observations.Count > 0)
        {
            yield return new ConversationMessage
            {
                Role = MessageRole.Assistant,
                Content = "Observations so far:\n"
                    + string.Join('\n', context.Observations.Select(o => $"- {o}"))
            };
        }
    }
}
