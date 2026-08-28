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
    private const string TransferPrefix = "TRANSFER:";
    private const string SayPrefix = "SAY:";

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

        context = FreshOfLastTurn(context);

        string verdict = await this.guardianService.ScreenAsync(context.Prompt);

        AgentContext? refused = await RefusedByGateAsync(context, verdict);

        if (refused is not null)
        {
            return refused;
        }

        context = await RoutedByGateAsync(context, verdict);

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

        (AgentContext Revising, string StatusNote)? rejected =
            await RejectedByGuardiansAsync(context, decided);

        if (rejected is not null)
        {
            return rejected.Value.Revising;
        }

        // Every guardian has passed, so this draft is an answer. It cannot be carrying a stale
        // Revising: both doors clear the last turn's verdict on entry, so Revising only ever
        // leaves this service when a guardian set it THIS turn.
        return decided;
    });

    // One verdict sequence for every final draft, whichever door it arrived through (SPEC.md
    // §7.6): the Judge on the merits, then the Contract on the shape — in that order,
    // deliberately, because a draft that is wrong on the merits should be told that, not told
    // its punctuation is off; two rejections in one turn would spend a turn teaching the model
    // the lesser of them. This method is the ONLY copy: the streamed contract hole existed
    // precisely because the sequence was written out once per door, so one door got the third
    // guardian and the other did not. Logging lives here too, so the two doors cannot drift in
    // what the trace shows.
    //
    // Returns the Revising context carrying the rejection feedback and a status note for the
    // stream to narrate, or null when the draft stands.
    private async ValueTask<(AgentContext Revising, string StatusNote)?> RejectedByGuardiansAsync(
        AgentContext context,
        AgentContext decided)
    {
        Judgement judgement =
            await this.guardianService.EvaluateAsync(task: context.Prompt, candidate: decided.Payload);

        string judgeOutcome = judgement.Score < MinimumAcceptableScore
            ? $"REJECT: {judgement.Reason}".TrimEnd(':', ' ')
            : "ACCEPT";

        await this.loggingBroker.LogProcessAsync(
            "Decision", $"Judge → scored {judgement.Score:F2} → {judgeOutcome}");

        if (judgement.Score < MinimumAcceptableScore)
        {
            AgentContext revising = context with
            {
                Observations =
                [
                    .. context.Observations,
                    RevisionFeedback(judgement, decided.Payload)
                ],

                Status = AgentStatus.Revising,

                // The rejected draft still cost a model call, and the loop bills the turn from
                // the context this method hands back — a revision loop the budget cannot see is
                // exactly where a run burns tokens fastest (SPEC.md §4.10).
                PromptTokens = decided.PromptTokens,
                CompletionTokens = decided.CompletionTokens,
                UsageIsEstimated = decided.UsageIsEstimated
            };

            return (revising, "judge rejected the draft; revising");
        }

        // The shape check runs after the Judge and not before it, deliberately: a draft that is
        // wrong on the merits should be told that, not told its punctuation is off. Two rejections
        // in one turn would spend a turn teaching the model the lesser of them.
        //
        // The schema is the one that SURVIVED precedence, riding the context from the boundary
        // (docs/per-request-inference.md §4.1). The configured field remains only for a context
        // built by hand, where it says exactly what the boundary would have seeded.
        ContractVerdict shape =
            await this.guardianService.CheckShapeAsync(
                decided.Payload,
                context.Inference?.ResponseSchemaJson ?? this.contractSchema);

        if (shape.Satisfied is false)
        {
            // A rejection the trace does not explain is a turn nobody can account for.
            await this.loggingBroker.LogProcessAsync(
                "Decision",
                $"Contract → REJECTED: {shape.Reason}");

            AgentContext revising = context with
            {
                Observations =
                [
                    .. context.Observations,
                    ShapeFeedback(shape, decided.Payload)
                ],

                Status = AgentStatus.Revising,
                PromptTokens = decided.PromptTokens,
                CompletionTokens = decided.CompletionTokens,
                UsageIsEstimated = decided.UsageIsEstimated
            };

            return (revising, "contract rejected the draft; revising");
        }

        return null;
    }

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

    // The streamed door, mapped like the batched one: a fault surfaces in the same family
    // ThinkAsync's TryCatch produces, whichever door the prompt entered by.
    public IDecisionStream ThinkStreamAsync(
        AgentContext context,
        CancellationToken cancellationToken = default) =>
        new DecisionStream(setResult =>
            MappedStreamAsync(
                StreamThinkAsync(context, setResult, cancellationToken),
                cancellationToken));

    private async IAsyncEnumerable<AgentStreamEvent> MappedStreamAsync(
        IAsyncEnumerable<AgentStreamEvent> stream,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using IAsyncEnumerator<AgentStreamEvent> segments =
            stream.GetAsyncEnumerator(cancellationToken);

        while (true)
        {
            AgentStreamEvent segment;

            try
            {
                if (await segments.MoveNextAsync() is false)
                {
                    break;
                }

                segment = segments.Current;
            }
            catch (Exception exception)
            {
                throw await MappedAndLoggedAsync(exception);
            }

            yield return segment;
        }
    }

    private async IAsyncEnumerable<AgentStreamEvent> StreamThinkAsync(
        AgentContext context,
        Action<AgentContext> setResult,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        context = FreshOfLastTurn(context);

        string verdict = await this.guardianService.ScreenAsync(context.Prompt);

        AgentContext? refused = await RefusedByGateAsync(context, verdict);

        if (refused is not null)
        {
            setResult(refused);

            yield return new AgentStreamEvent(
                AgentStreamEventType.Status, "gate refused the request");

            yield break;
        }

        context = await RoutedByGateAsync(context, verdict);

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

        // The identical verdict sequence the batched door runs — one copy, so neither door can
        // hold a guardian the other lacks.
        (AgentContext Revising, string StatusNote)? rejected =
            await RejectedByGuardiansAsync(context, decided);

        if (rejected is not null)
        {
            setResult(rejected.Value.Revising);

            yield return new AgentStreamEvent(
                AgentStreamEventType.Status, rejected.Value.StatusNote);

            yield break;
        }

        // The shape check, exactly as the batched path runs it — the streamed loop used to skip
        // it, which made streaming the way to receive an answer the Contract would have refused.
        // A control a caller can step around by changing method is not a control (SPEC.md §7.6).
        ContractVerdict shape =
            await this.guardianService.CheckShapeAsync(
                decided.Payload,
                context.Inference?.ResponseSchemaJson ?? this.contractSchema);

        if (shape.Satisfied is false)
        {
            await this.loggingBroker.LogProcessAsync(
                "Decision",
                $"Contract → REJECTED: {shape.Reason}");

            setResult(context with
            {
                Observations =
                [
                    .. context.Observations,
                    ShapeFeedback(shape, decided.Payload)
                ],

                Status = AgentStatus.Revising
            });

            yield return new AgentStreamEvent(
                AgentStreamEventType.Status, "contract rejected the draft; revising");

            yield break;
        }

        setResult(decided.Status is AgentStatus.Revising
            ? decided with { Status = AgentStatus.Working }
            : decided);
    }

    // The Gate's refusal, handled once for both doors: the verdict never becomes the answer —
    // it is recorded as RawReply and the reply is the fixed refusal message. Null when the Gate
    // did not refuse. Logged here so both doors narrate the same line.
    private async ValueTask<AgentContext?> RefusedByGateAsync(AgentContext context, string verdict)
    {
        if (IsRefusal(verdict) is false)
        {
            return null;
        }

        await this.loggingBroker.LogProcessAsync(
            "Decision", $"Gate → REFUSE: {verdict.ReplaceLineEndings(" ").Trim()}");

        return context with
        {
            Intent = RefuseDirection,
            DirectionType = RefuseDirection,
            Payload = RefusalMessage,
            RawReply = verdict
        };
    }

    // Everything else the Gate's verdict carries, handled once for both doors. Invariant 6
    // holds structurally either way — a verdict is only ever read as a classification, so a
    // Gate that emits FINAL: or ACTION: falls through to the Brain and its text never becomes
    // the answer — but the overreach is recorded (§7.6), because a guardian trying to answer is
    // exactly the event a security review needs to see. A route label rides through as Data;
    // this used to happen on the streamed door only, so a routing Gate steered skill selection
    // for streaming callers and was silently ignored for batched ones.
    private async ValueTask<AgentContext> RoutedByGateAsync(AgentContext context, string verdict)
    {
        if (IsGuardianOverreach(verdict))
        {
            await LogGuardianOverreachAsync();
        }

        await this.loggingBroker.LogProcessAsync(
            "Decision",
            IsRoute(verdict)
                ? $"Gate → ROUTE: {verdict.ReplaceLineEndings(" ").Trim()}"
                : $"Gate → ACCEPT: {verdict.ReplaceLineEndings(" ").Trim()}");

        return IsRoute(verdict)
            ? context with { Route = ExtractRouteLabel(verdict) }
            : context;
    }

    // Status and cost are Decision's OUTPUTS: what THIS turn's guardians concluded, and what
    // THIS turn's model calls consumed. The incoming context may still carry the previous
    // turn's — Interpret builds every decided context with `context with { ... }`, which copies
    // both forward — and either one leaking back out is a shipped defect: a stale Revising made
    // the loop skip Direction and spin (an accepted draft refused at the cap; a tool call
    // proposed after a rejection swallowed), and a stale token count made MeasuredAsync read
    // "the provider already reported" on every turn after the first, so the text protocol was
    // billed turn 1's figure forever. Cleared once, at both doors, so no branch below can leak
    // either.
    private static AgentContext FreshOfLastTurn(AgentContext context) =>
        context with
        {
            Status = context.Status is AgentStatus.Revising
                ? AgentStatus.Working
                : context.Status,

            PromptTokens = 0,
            CompletionTokens = 0,
            UsageIsEstimated = false,

            // Narration is a turn's output too: a refusal or conflict path below bypasses
            // Interpret, and last turn's prose leaking out would be voiced over this turn.
            Narration = ""
        };

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

    // SAY: belongs here too — narration is a voice, and a guardian is never a voice.
    private static bool IsGuardianOverreach(string verdict) =>
        verdict.Contains(FinalPrefix, StringComparison.OrdinalIgnoreCase)
            || verdict.Contains(ActionPrefix, StringComparison.OrdinalIgnoreCase)
            || verdict.Contains(ToolPrefix, StringComparison.OrdinalIgnoreCase)
            || verdict.Contains(TransferPrefix, StringComparison.OrdinalIgnoreCase)
            || verdict.Contains(SayPrefix, StringComparison.OrdinalIgnoreCase);

    private static string ExtractRouteLabel(string verdict)
    {
        string label = verdict.TrimStart()[RouteVerdict.Length..];

        return label.TrimStart(':', ' ').ReplaceLineEndings(" ").Trim();
    }

}
