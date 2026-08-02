// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Models.Clients.Agents;
using Standard.Agents.Models.Foundations.Gates;
using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Services.Foundations.Brains;
using Standard.Agents.Services.Foundations.Gates;
using Standard.Agents.Services.Foundations.Judges;

namespace Standard.Agents.Services.Orchestrations.Decision;

public partial class DecisionOrchestrationService : IDecisionOrchestrationService
{
    private const string ActionPrefix = "ACTION:";
    private const string ToolPrefix = "TOOL:";
    private const string FinalPrefix = "FINAL:";
    private const string RefuseVerdict = "refuse";
    private const string RefuseDirection = "Refuse";
    private const string ReturnResponseDirection = "ReturnResponse";
    private const string RespondIntent = "Respond";
    private const string AwaitInputDirection = "AwaitInput";
    private const string ConflictPrefix = "CONFLICT:";
    private const string ConflictOptionSeparator = "||";
    private const string ConflictLabelSeparator = "|";
    private const string PreferencePrefix = "SKILL_PREFERENCE::";
    private const string RefusalMessage = "I'm not able to help with that.";

    private const double MinimumAcceptableScore = 0.3;

    private readonly IGateService gateService;
    private readonly IBrainService brainService;
    private readonly IJudgeService judgeService;
    private readonly ILoggingBroker loggingBroker;

    public DecisionOrchestrationService(
        IGateService gateService,
        IBrainService brainService,
        IJudgeService judgeService,
        ILoggingBroker loggingBroker)
    {
        this.gateService = gateService;
        this.brainService = brainService;
        this.judgeService = judgeService;
        this.loggingBroker = loggingBroker;
    }

    public ValueTask<AgentContext> ThinkAsync(AgentContext context) =>
    TryCatch(async () =>
    {
        ValidateContext(context);

        string verdict =
            await this.gateService.ScreenAsync(context.Prompt);

        if (IsRefusal(verdict))
        {
            return context with
            {
                Intent = RefuseDirection,
                DirectionType = RefuseDirection,
                Payload = RefusalMessage,
                RawReply = verdict
            };
        }

        (AgentContext resolvedContext, bool isTerminal) =
            await ResolveSkillConflictAsync(context);

        if (isTerminal)
        {
            return resolvedContext;
        }

        context = resolvedContext;

        string reply =
            (await this.brainService.GenerateAsync(
                systemPrompt: context.SystemPrompt,
                userPrompt: BuildUserMessage(context))).Trim();

        AgentContext decided = Interpret(context, reply);

        bool isFinalAnswer =
            decided.DirectionType.Equals(
                ReturnResponseDirection,
                StringComparison.OrdinalIgnoreCase);

        if (isFinalAnswer is false)
        {
            return decided;
        }

        // An empty answer is nothing to judge — a judge would (rightly) reject blank input,
        // which would crash the turn. Return it as-is; the brain simply had nothing to say.
        if (string.IsNullOrWhiteSpace(decided.Payload))
        {
            return decided;
        }

        double score =
            await this.judgeService.EvaluateAsync(candidate: decided.Payload);

        if (score < MinimumAcceptableScore)
        {
            return context with
            {
                Observations =
                [
                    .. context.Observations,
                    $"A previous draft was rejected on review: {decided.Payload}"
                ],

                Status = AgentStatus.Revising
            };
        }

        return decided;
    });

    public IDecisionStream ThinkStreamAsync(
        AgentContext context,
        CancellationToken cancellationToken = default) =>
        new DecisionStream(setResult =>
            StreamThinkAsync(context, setResult, cancellationToken));

    private async IAsyncEnumerable<AgentStreamEvent> StreamThinkAsync(
        AgentContext context,
        Action<AgentContext> setResult,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        string verdict = await this.gateService.ScreenAsync(context.Prompt);

        if (IsRefusal(verdict))
        {
            await this.loggingBroker.LogProcessAsync(
                "Decision", $"Gate → REFUSE: {verdict.ReplaceLineEndings(" ").Trim()}");

            setResult(context with
            {
                Intent = RefuseDirection,
                DirectionType = RefuseDirection,
                Payload = RefusalMessage,
                RawReply = verdict
            });

            yield return new AgentStreamEvent(
                AgentStreamEventType.Status, "gate refused the request");

            yield break;
        }

        await this.loggingBroker.LogProcessAsync(
            "Decision", $"Gate → PASS: {verdict.ReplaceLineEndings(" ").Trim()}");

        (AgentContext resolvedContext, bool isTerminal) =
            await ResolveSkillConflictAsync(context);

        if (isTerminal)
        {
            setResult(resolvedContext);

            yield return new AgentStreamEvent(
                AgentStreamEventType.Status,
                resolvedContext.DirectionType == AwaitInputDirection
                    ? "skills conflict; asking for clarification"
                    : "learned your skill preference");

            yield break;
        }

        context = resolvedContext;

        var classifier = new ReplyStreamClassifier();
        var reply = new StringBuilder();

        IAsyncEnumerable<string> tokens = this.brainService.GenerateStreamAsync(
            systemPrompt: context.SystemPrompt,
            userPrompt: BuildUserMessage(context),
            cancellationToken: cancellationToken);

        await foreach (string delta in tokens.WithCancellation(cancellationToken))
        {
            reply.Append(delta);

            foreach (AgentStreamEvent segment in classifier.Classify(delta))
            {
                yield return AsUnsettledDraft(segment);
            }
        }

        foreach (AgentStreamEvent segment in classifier.Flush())
        {
            yield return AsUnsettledDraft(segment);
        }

        AgentContext decided = Interpret(context, reply.ToString().Trim());

        await this.loggingBroker.LogProcessAsync(
            "Decision", $"Brain → replied ({reply.Length} chars)", detail: true);

        await this.loggingBroker.LogProcessAsync(
            "Decision", $"Interpreted → {decided.DirectionType}");

        bool isFinalAnswer = decided.DirectionType.Equals(
            ReturnResponseDirection, StringComparison.OrdinalIgnoreCase);

        if (isFinalAnswer is false)
        {
            setResult(decided);

            yield return new AgentStreamEvent(
                AgentStreamEventType.Status, $"using tool: {decided.DirectionType}");

            yield break;
        }

        if (string.IsNullOrWhiteSpace(decided.Payload))
        {
            setResult(decided);

            yield break;
        }

        double score = await this.judgeService.EvaluateAsync(candidate: decided.Payload);

        await this.loggingBroker.LogProcessAsync(
            "Decision",
            $"Judge → scored {score:F2} → {(score < MinimumAcceptableScore ? "REJECT" : "ACCEPT")}");

        if (score < MinimumAcceptableScore)
        {
            setResult(context with
            {
                Observations =
                [
                    .. context.Observations,
                    $"A previous draft was rejected on review: {decided.Payload}"
                ],

                Status = AgentStatus.Revising
            });

            yield return new AgentStreamEvent(
                AgentStreamEventType.Status, "judge rejected the draft; revising");

            yield break;
        }

        setResult(decided);
    }

    private static AgentStreamEvent AsUnsettledDraft(AgentStreamEvent segment) =>
        segment.Type is AgentStreamEventType.Response
            ? segment with { Type = AgentStreamEventType.Thinking }
            : segment;

    private async ValueTask<(AgentContext Context, bool IsTerminal)> ResolveSkillConflictAsync(
        AgentContext context)
    {
        if (string.IsNullOrWhiteSpace(context.SystemPrompt))
        {
            return (context, false);
        }

        string verdict = await this.gateService.DetectConflictAsync(context.SystemPrompt);
        SkillConflict? conflict = ParseConflict(verdict);

        if (conflict is null)
        {
            return (context, false);
        }

        string key = ConflictKey(conflict);
        string? preferred = FindPreference(context.Observations, key);

        if (preferred is not null)
        {
            AgentContext resolvedContext = context with
            {
                Observations =
                [
                    .. context.Observations,
                    $"The user resolved a skill conflict in favor of '{preferred}'; "
                        + "follow it and ignore the conflicting instruction."
                ]
            };

            return (resolvedContext, false);
        }

        SkillDirective? chosen = MatchChoice(context.Prompt, conflict);

        if (chosen is not null)
        {
            AgentContext learnedContext = context with
            {
                Intent = RespondIntent,
                DirectionType = ReturnResponseDirection,
                Payload = $"Understood — I'll follow '{chosen.Skill}' for that from now on.",
                Remember = $"{PreferencePrefix}{key}::{chosen.Skill}",
                RawReply = verdict
            };

            return (learnedContext, true);
        }

        AgentContext questionContext = context with
        {
            Intent = AwaitInputDirection,
            DirectionType = AwaitInputDirection,
            Payload = BuildClarificationQuestion(conflict),
            RawReply = verdict
        };

        return (questionContext, true);
    }

    private static string ConflictKey(SkillConflict conflict) =>
        string.Join(
            "|",
            conflict.Options
                .Select(option => option.Skill.Trim().ToLowerInvariant())
                .OrderBy(label => label));

    private static string? FindPreference(IReadOnlyList<string> observations, string key)
    {
        string prefix = $"{PreferencePrefix}{key}::";

        string? record = observations.LastOrDefault(observation =>
            observation.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

        return record?[prefix.Length..];
    }

    private static SkillDirective? MatchChoice(string prompt, SkillConflict conflict) =>
        conflict.Options.FirstOrDefault(option =>
            prompt.Contains(option.Skill, StringComparison.OrdinalIgnoreCase));

    private static SkillConflict? ParseConflict(string verdict)
    {
        string trimmed = (verdict ?? string.Empty).Trim();

        if (trimmed.StartsWith(ConflictPrefix, StringComparison.OrdinalIgnoreCase) is false)
        {
            return null;
        }

        List<SkillDirective> options = trimmed[ConflictPrefix.Length..]
            .Split(
                ConflictOptionSeparator,
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ToDirective)
            .ToList();

        return options.Count >= 2 ? new SkillConflict(options) : null;
    }

    private static SkillDirective ToDirective(string option)
    {
        string[] parts = option.Split(
            ConflictLabelSeparator, count: 2, StringSplitOptions.TrimEntries);

        return parts.Length == 2
            ? new SkillDirective(parts[0], parts[1])
            : new SkillDirective(option, option);
    }

    private static string BuildClarificationQuestion(SkillConflict conflict)
    {
        string choices = string.Join(" or ", conflict.Options.Select(option => option.Skill));

        return $"Your skills give conflicting instructions. Should I follow: {choices}?";
    }

    private static bool IsRefusal(string verdict) =>
verdict.TrimStart().StartsWith(RefuseVerdict, StringComparison.OrdinalIgnoreCase);

    private static string BuildUserMessage(AgentContext context)
    {
        StringBuilder userMessage = new();

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
