// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Text;
using System.Text.Json;
using Standard.Agents.Models.Brokers.Sessions;
using Standard.Agents.Models.Orchestrations.Agents;

namespace Standard.Agents.Services.Orchestrations.Decision.Inferences;

// The reply protocol (SPEC.md 6). What the model is shown, and how its answer is read back into
// an intent. Both halves belong to Inference: they are the shape of one model call.
public partial class InferenceOrchestrationService
{
    private static string BuildUserMessage(AgentContext context)
    {
        StringBuilder userMessage = new();

        // What was said before, oldest first, so a follow-up resolves against it rather than
        // starting from nothing (SPEC.md §4.11). Absent a session this is empty and the message
        // is exactly what it always was.
        if (context.History.Count > 0)
        {
            userMessage.AppendLine("Conversation so far:").AppendLine();

            foreach (AgentTurn turn in context.History)
            {
                userMessage.Append("User: ").AppendLine(turn.Prompt);
                userMessage.Append("You: ").AppendLine(turn.Answer);
            }

            userMessage.AppendLine();
        }

        userMessage.Append("Task: ").Append(context.Prompt);

        if (context.Observations.Count > 0)
        {
            userMessage.AppendLine().AppendLine().AppendLine("Observations so far:");

            foreach (string observation in context.Observations)
            {
                userMessage.Append("- ").AppendLine(observation);
            }
        }

        return userMessage.ToString();
    }

    private static AgentContext Interpret(AgentContext context, string reply)
    {
        string firstLine = reply.Split('\n')[0].Trim();

        if (firstLine.StartsWith(ToolPrefix, StringComparison.OrdinalIgnoreCase)
            && TryParseToolCall(firstLine[ToolPrefix.Length..], out string calledTool, out string arguments))
        {
            return context with
            {
                Intent = calledTool,
                DirectionType = calledTool,
                Payload = arguments,
                RawReply = reply
            };
        }

        bool modelChoseToAct =
            firstLine.StartsWith(ActionPrefix, StringComparison.OrdinalIgnoreCase);

        if (modelChoseToAct)
        {
            string[] toolCall =
                firstLine[ActionPrefix.Length..]
                .Split(':', 2, StringSplitOptions.TrimEntries);

            string toolName = toolCall[0];
            string toolInput = toolCall.Length > 1 ? toolCall[1] : string.Empty;

            // A model can emit the "ACTION:" prefix with no tool name behind it (small
            // models parrot the protocol template). That is not a tool call — fall
            // through and treat the reply as the answer rather than routing an empty
            // tool name into Direction, where it would fault.
            if (string.IsNullOrWhiteSpace(toolName) is false)
            {
                return context with
                {
                    Intent = toolName,
                    DirectionType = toolName,
                    Payload = toolInput,
                    RawReply = reply
                };
            }
        }

        string answer = reply.StartsWith(FinalPrefix, StringComparison.OrdinalIgnoreCase)
            ? reply[FinalPrefix.Length..].Trim()
            : reply;

        return context with
        {
            Intent = RespondIntent,
            DirectionType = ReturnResponseDirection,
            Payload = answer,
            RawReply = reply
        };
    }

    private static bool TryParseToolCall(string json, out string toolName, out string arguments)
    {
        toolName = string.Empty;
        arguments = string.Empty;

        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;

            if (root.ValueKind is not JsonValueKind.Object)
            {
                return false;
            }

            if (root.TryGetProperty("tool", out JsonElement toolElement) is false
                || toolElement.ValueKind is not JsonValueKind.String)
            {
                return false;
            }

            string parsedTool = toolElement.GetString() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(parsedTool))
            {
                return false;
            }

            toolName = parsedTool;

            arguments = root.TryGetProperty("arguments", out JsonElement argumentsElement)
                ? argumentsElement.GetRawText()
                : "{}";

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
