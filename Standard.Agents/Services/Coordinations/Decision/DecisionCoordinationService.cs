// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Text.Json;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Models.Clients.Agents;
using Standard.Agents.Models.Foundations.Gates;
using Standard.Agents.Models.Foundations.Contracts;
using Standard.Agents.Models.Foundations.Judges;
using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Services.Orchestrations.Decision.Guardians;
using Standard.Agents.Services.Orchestrations.Decision.Inferences;

namespace Standard.Agents.Services.Coordinations.Decision;

// The Decision nature: two regions, and the judgment between them.
//
// Inference asks the model and reads its answer; Guardian screens what goes in and scores what
// comes out. The loop that runs them — screen, resolve a skill conflict, decide, judge, revise —
// belongs to neither, because every step of it depends on the other region's result.
public partial class DecisionCoordinationService : IDecisionCoordinationService
{
    // A guardian that emits one of these is trying to ANSWER rather than classify. Invariant 6
    // holds structurally either way - a verdict is only ever read as a classification - but the
    // attempt is recorded, because it is exactly the event a security review needs to see.
    private const string ActionPrefix = "ACTION:";
    private const string ToolPrefix = "TOOL:";
    private const string FinalPrefix = "FINAL:";

    private const string RefuseVerdict = "refuse";
    private const string RouteVerdict = "route";
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

    private readonly IInferenceOrchestrationService inferenceService;
    private readonly IGuardianOrchestrationService guardianService;
    private readonly ILoggingBroker loggingBroker;

    // The shape every answer is held to, or empty. Configuration rather than a collaborator — it
    // is a string the host chose, and the foundation treats an empty one as no check at all.
    private readonly string contractSchema;

    public DecisionCoordinationService(
        IInferenceOrchestrationService inferenceService,
        IGuardianOrchestrationService guardianService,
        ILoggingBroker loggingBroker,
        string? contractSchema = null)
    {
        this.inferenceService = inferenceService;
        this.guardianService = guardianService;
        this.loggingBroker = loggingBroker;
        this.contractSchema = contractSchema ?? string.Empty;
    }

    // The loop screens what a tool returned before it can become an observation. It asks here
    // because the Gate is Decision's foundation, and an instruction arriving inside data is the
    // same category of thing as an instruction arriving in a prompt.
    public ValueTask<string> ScreenAsync(string text) =>
        this.guardianService.ScreenAsync(text);

    public ValueTask<AgentContext> ThinkAsync(AgentContext context) =>
    TryCatch(async () =>
    {
        ValidateContext(context);

        string verdict = await this.guardianService.ScreenAsync(context.Prompt);

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

        // Invariant 6 holds structurally on both paths — a verdict is only ever read as a
        // classification, so a Gate that emits FINAL: or ACTION: falls through to the Brain and
        // its text never becomes the answer. What the batch path was missing is the record:
        // §7.6 says overreach SHOULD be recorded, and a guardian trying to answer is exactly
        // the event a security review needs to see. Streaming already logged it; now both do.
        if (IsGuardianOverreach(verdict))
        {
            await LogGuardianOverreachAsync();
        }

        (AgentContext resolvedContext, bool isTerminal) =
            await ResolveSkillConflictAsync(context);

        if (isTerminal)
        {
            return resolvedContext;
        }

        context = resolvedContext;

        AgentContext decided = await this.inferenceService.DecideAsync(context);

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

        Judgement judgement =
            await this.guardianService.EvaluateAsync(task: context.Prompt, candidate: decided.Payload);

        if (judgement.Score < MinimumAcceptableScore)
        {
            return context with
            {
                Observations =
                [
                    .. context.Observations,
                    RevisionFeedback(judgement, decided.Payload)
                ],

                Status = AgentStatus.Revising
            };
        }

        // The shape check runs after the Judge and not before it, deliberately: a draft that is
        // wrong on the merits should be told that, not told its punctuation is off. Two rejections
        // in one turn would spend a turn teaching the model the lesser of them.
        ContractVerdict shape =
            await this.guardianService.CheckShapeAsync(decided.Payload, this.contractSchema);

        if (shape.Satisfied is false)
        {
            // A rejection the trace does not explain is a turn nobody can account for. The Judge's
            // rejections were already narrated; this one says which guardian refused and why.
            await this.loggingBroker.LogProcessAsync(
                "Decision",
                $"Contract → REJECTED: {shape.Reason}");

            return context with
            {
                Observations =
                [
                    .. context.Observations,
                    ShapeFeedback(shape, decided.Payload)
                ],

                Status = AgentStatus.Revising
            };
        }

        // Every guardian has passed, so this draft is an answer — and it must stop carrying the
        // Revising it inherited from the turn that rejected the last one. Interpret builds the
        // decided context with `context with { ... }`, which copies Status forward, so without
        // this a draft accepted on the second pass leaves Decision still marked Revising, the loop
        // continues, and the run exhausts its turns refusing an answer that had already passed.
        return decided.Status is AgentStatus.Revising
            ? decided with { Status = AgentStatus.Working }
            : decided;
    });

    // Aimed, not scolding. A revision the model cannot act on is a turn spent for nothing, so the
    // validator's complaint is repeated verbatim rather than summarised into "invalid".
    private static string ShapeFeedback(ContractVerdict verdict, string draft) =>
        $"A previous draft was rejected because {verdict.Reason}. Reply with JSON matching the "
            + $"required shape and nothing else. The draft was: {draft}";

    private static string RevisionFeedback(Judgement judgement, string draft) =>
        string.IsNullOrWhiteSpace(judgement.Reason)
            ? $"A previous draft was rejected on review: {draft}"
            : $"A previous draft was rejected on review — {judgement.Reason}. "
                + $"The draft was: {draft}";

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
        string verdict = await this.guardianService.ScreenAsync(context.Prompt);

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

        if (IsGuardianOverreach(verdict))
        {
            await LogGuardianOverreachAsync();
        }

        await this.loggingBroker.LogProcessAsync(
            "Decision",
            IsRoute(verdict)
                ? $"Gate → ROUTE: {verdict.ReplaceLineEndings(" ").Trim()}"
                : $"Gate → ACCEPT: {verdict.ReplaceLineEndings(" ").Trim()}");

        if (IsRoute(verdict))
        {
            context = context with { Route = ExtractRouteLabel(verdict) };
        }

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

        AgentContext decided = context;

        IAsyncEnumerable<AgentStreamEvent> drafting =
            this.inferenceService.DecideStreamAsync(
                context,
                setDecided: interpreted => decided = interpreted,
                cancellationToken: cancellationToken);

        await foreach (AgentStreamEvent segment in drafting.WithCancellation(cancellationToken))
        {
            yield return segment;
        }

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

        Judgement judgement = await this.guardianService.EvaluateAsync(task: context.Prompt, candidate: decided.Payload);

        string judgeOutcome = judgement.Score < MinimumAcceptableScore
            ? $"REJECT: {judgement.Reason}".TrimEnd(':', ' ')
            : "ACCEPT";

        await this.loggingBroker.LogProcessAsync(
            "Decision", $"Judge → scored {judgement.Score:F2} → {judgeOutcome}");

        if (judgement.Score < MinimumAcceptableScore)
        {
            setResult(context with
            {
                Observations =
                [
                    .. context.Observations,
                    RevisionFeedback(judgement, decided.Payload)
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

        string verdict = await this.guardianService.DetectConflictAsync(context.SystemPrompt);
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

    private static bool IsRoute(string verdict) =>
        verdict.TrimStart().StartsWith(RouteVerdict, StringComparison.OrdinalIgnoreCase);

    private ValueTask LogGuardianOverreachAsync() =>
        this.loggingBroker.LogProcessAsync(
            "Decision",
            "Gate overreach — a guardian tried to answer or act instead of classifying; "
                + "neutralized and passed to the Brain "
                + "(Invariant 6: a guardian is never the Brain)");

    private static bool IsGuardianOverreach(string verdict) =>
        verdict.Contains(FinalPrefix, StringComparison.OrdinalIgnoreCase)
            || verdict.Contains(ActionPrefix, StringComparison.OrdinalIgnoreCase)
            || verdict.Contains(ToolPrefix, StringComparison.OrdinalIgnoreCase);

    private static string ExtractRouteLabel(string verdict)
    {
        string label = verdict.TrimStart()[RouteVerdict.Length..];

        return label.TrimStart(':', ' ').ReplaceLineEndings(" ").Trim();
    }

}
