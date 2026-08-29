// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Approvals;
using Standard.Agents.Models.Loggings;
using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Models.Orchestrations.Effects;

namespace Standard.Agents.Services.Coordinations.Direction;

// The perimeter (SPEC.md §4.9). Direction already owned the boundary; this is enforcement at
// it, in the order the spec fixes and forbids reordering:
//
//   authorize → record the intent → approve → execute at most once → record the outcome
//
// The order is the control. Authorizing after execution audits a fait accompli; recording the
// intent after execution loses the effects that crashed mid-flight; and approving after
// execution is not approval at all.
public partial class DirectionCoordinationService
{
    private async ValueTask<AgentContext> ActOnEffectAsync(AgentContext context)
    {
        // 0 — the offering (SPEC.md §4.15, enforced). Selection narrows what a run is SHOWN;
        // with enforcement on, the offering also binds here: an advertised tool the run was not
        // offered is denied before it becomes an act, because a Brain outside this loop's
        // mediation can carry side-channel knowledge of the catalog and name a tool the run was
        // never shown. Caller tools never reach this method (classified before the perimeter),
        // and an undescribed tool keeps its §6.1 treatment.
        if (DeniedBecauseSelectionWithheldIt(context.DirectionType))
        {
            string withheld =
                $"tool '{context.DirectionType}' was not offered to this run: selection "
                    + "withheld it. Choose among the offered tools or answer directly.";

            await this.loggingBroker.LogProcessAsync(
                "Direction",
                $"Selection → DENIED '{context.DirectionType}': not offered to this run");

            return Denied(context, withheld);
        }

        AgentEffect effect = AgentEffect.For(
            runId: AgentRun.Current?.Id ?? string.Empty,
            toolName: context.DirectionType,
            arguments: context.Payload,
            riskLevel: RiskLevelFor(context.DirectionType),
            approvalRequired: RequiresApproval(context.DirectionType),
            scope: ScopeFor(context.DirectionType, context.Payload),

            // Who is acting, asked at the moment the act is described — so the policy broker
            // deciding whether it may happen is told, not merely the record written afterwards
            // (SPEC.md §4.9). Null when the host configured no identity, which claims nothing
            // rather than inventing someone.
            principal: this.identityResolver?.Invoke());

        // 1 — authorize
        AuthorizationDecision decision = await this.perimeterService.AuthorizeAsync(effect);

        if (decision.Permitted is false)
        {
            await this.loggingBroker.LogProcessAsync(
                "Direction",
                $"Policy → DENIED '{effect.ToolName}': {decision.Reason}");

            return Denied(context, decision.Reason);
        }

        // 1b — the mode's answer for an act nothing named, under Deny. It is decided here with
        // authorization and not at the approval stage, because Deny asks nobody: the act is
        // refused before its intent is recorded, exactly as a policy denial is — told,
        // non-terminal, recoverable. An act RequireApproval names was mentioned, so it travels
        // to its authority below; the mode speaks only for what no permission mentioned.
        if (DeniedBecauseNothingPermitsIt(effect))
        {
            string denial =
                $"tool '{effect.ToolName}' is not permitted: nothing explicitly permits it "
                    + "and the permission mode is Deny";

            await this.loggingBroker.LogProcessAsync(
                "Direction",
                $"Permissions → DENIED '{effect.ToolName}': nothing explicitly permits it");

            return Denied(context, denial);
        }

        // 2 — record the intent, and learn whether this act already happened
        string? priorOutcome = await this.perimeterService.ClaimAsync(effect);

        if (priorOutcome is not null)
        {
            await this.loggingBroker.LogProcessAsync(
                "Direction",
                $"Run-once → '{effect.ToolName}' already ran; replaying its outcome");

            return Observed(context, priorOutcome);
        }

        // 3 — approve, if required
        //
        // Required by name, or by the mode: Ask is the disposition toward everything nobody
        // mentioned, which is the only workable posture for an agent whose targets cannot be
        // enumerated at composition.
        if (effect.ApprovalRequired || AskBecauseNothingPermittedIt(effect))
        {
            // An authority asked the identical question twice stops reading it. A grant is
            // remembered for the tool AND the scope it was given for, exactly — approving a write
            // to one file is not approving writes to every file, and a broader grant is a
            // judgement only the authority can make.
            //
            // Exactly, which is why an act with no named scope remembers nothing: a tool that
            // does not say what it touches (ScopeOf unimplemented — every MCP tool among them)
            // leaves nothing for a later act to match, and remembering the empty scope collapsed
            // the key to the tool name — approving a $10 transfer silently approved the $10,000
            // one. Each such act is its own question; an identical repeat is already replayed by
            // run-once above, before approval is ever reached.
            bool scopeIsNamed = string.IsNullOrEmpty(effect.Scope) is false;

            if (scopeIsNamed
                && AgentRun.Current?.WasGranted(effect.ToolName, effect.Scope) is true)
            {
                await this.loggingBroker.LogProcessAsync(
                    "Direction",
                    $"Approval → already granted '{effect.ToolName}' at '{effect.Scope}'");
            }
            else
            {
                ApprovalDecision approval =
                    await this.perimeterService.RequestApprovalAsync(effect);

                if (approval is not ApprovalDecision.Approved)
                {
                    return await HandleUnapprovedAsync(context, effect, approval);
                }

                if (scopeIsNamed)
                {
                    AgentRun.Current?.RememberGrant(effect.ToolName, effect.Scope);
                }

                await this.loggingBroker.LogProcessAsync(
                    "Direction", $"Approval → APPROVED '{effect.ToolName}'");
            }
        }

        // 4 — execute
        string output = await RunToolAsync(context);

        // 5 — record the outcome, before the loop advances
        await this.perimeterService.RecordOutcomeAsync(effect, output);

        // Remember that this run performed it, so it can be unwound. Only here: an effect denied,
        // held for approval, or replayed from the ledger returned above and was never performed by
        // this run, and compensating it would undo something this run did not do (SPEC.md §4.9).
        AgentRun.Current?.RecordPerformed(
            new PerformedEffect(effect.ToolName, effect.Arguments, output));

        await this.loggingBroker.LogProcessAsync(
            "Direction", $"Tool '{effect.ToolName}' ← {context.Payload}", detail: true);

        await this.loggingBroker.LogProcessAsync(
            "Direction", $"Tool '{effect.ToolName}' → {output}");

        return Observed(context, output);
    }

    private async ValueTask<AgentContext> HandleUnapprovedAsync(
        AgentContext context,
        AgentEffect effect,
        ApprovalDecision approval)
    {
        // The act was held, not performed, so the claim taken when its intent was recorded is
        // given back (SPEC.md §4.9). Leaving it standing would make the approval unusable when it
        // finally arrives: the authority says yes, the resumed run proposes the act, and the
        // ledger reports it as already done.
        await this.perimeterService.ReleaseClaimAsync(effect);

        if (approval is ApprovalDecision.Denied)
        {
            string denial = $"approval denied for '{effect.ToolName}'";

            await this.loggingBroker.LogProcessAsync(
                "Direction", $"Approval → DENIED '{effect.ToolName}'; the claim was released");

            return Denied(context, denial);
        }

        // Pending. The act is held, not performed — waiting is not consent (SPEC.md §4.9).
        await this.loggingBroker.LogProcessAsync(
            "Direction",
            $"Approval → PENDING '{effect.ToolName}'; the effect was not performed "
                + "and the claim was released");

        return context with
        {
            Result = $"'{effect.ToolName}' is waiting for approval before it can run.",

            // The act travels with the pause, so whoever resumes can be shown what they are
            // permitting rather than only that something is waiting (SPEC.md §4.11).
            PendingEffect = effect,
            Status = AgentStatus.AwaitingApproval
        };
    }

    // Compensation (SPEC.md §4.9, Invariant 7). Run-once makes an effect safe to PROPOSE twice;
    // compensation is for the effects that cannot be made idempotent at all — a payment sent, a
    // message delivered — where the only way back is a second, opposite act.
    //
    // It unwinds in reverse order, because a later effect may depend on an earlier one: undoing
    // the booking before the payment it paid for would leave the payment attached to nothing.
    public async ValueTask<IReadOnlyList<CompensationOutcome>> CompensateRunAsync()
    {
        AgentRun? run = AgentRun.Current;

        if (run is null)
        {
            return [];
        }

        List<CompensationOutcome> outcomes = [];

        foreach (PerformedEffect effect in run.PerformedEffects.Reverse())
        {
            CompensationOutcome outcome = await CompensateEffectAsync(effect);

            await this.loggingBroker.LogProcessAsync(
                "Direction",
                $"Compensate '{effect.ToolName}' → "
                    + $"{(outcome.Undone ? "UNDONE" : "STANDS")}: {outcome.Detail}");

            outcomes.Add(outcome);
        }

        return outcomes;
    }

    // Best effort per effect. A reversal is itself an act against the world, and acts fail;
    // abandoning the rest because this one refused would leave more standing, not less.
    private async ValueTask<CompensationOutcome> CompensateEffectAsync(PerformedEffect effect)
    {
        string stands = $"'{effect.ToolName}' could not be undone; the effect stands.";

        try
        {
            string reversal = await this.executionService.CompensateAsync(
                effect.ToolName, effect.Arguments, effect.Outcome);

            return string.IsNullOrEmpty(reversal)
                ? new CompensationOutcome(effect.ToolName, Undone: false, Detail: stands)
                : new CompensationOutcome(effect.ToolName, Undone: true, Detail: reversal);
        }
        catch (Exception)
        {
            return new CompensationOutcome(effect.ToolName, Undone: false, Detail: stands);
        }
    }

    private ValueTask<string> RunToolAsync(AgentContext context) =>
        this.executionService.RunAsync(context.DirectionType, context.Payload);

    // A denial is non-terminal: the agent is told and may choose a permitted path on the next
    // turn, exactly as it recovers from a malformed call (SPEC.md §4.6, §4.9).
    private static AgentContext Denied(AgentContext context, string reason) =>
        context with
        {
            Result = reason,
            Observations = [.. context.Observations, $"{context.DirectionType}: {reason}"],
            ToolExchanges = WithExchange(context, reason),
            Status = AgentStatus.Working
        };

    // A call the model made gets an answer, whatever the answer is. A denial and a withheld
    // result are answers; leaving the call unanswered would strand it, and some providers reject
    // a conversation whose tool call has no matching tool message (SPEC.md §6).
    private static IReadOnlyList<ToolExchange> WithExchange(AgentContext context, string result) =>
        string.IsNullOrEmpty(context.ToolCallId)
            ? context.ToolExchanges
            : [.. context.ToolExchanges,
                new ToolExchange(
                    context.ToolCallId, context.DirectionType, context.Payload, result)];

    private static AgentContext Observed(AgentContext context, string output) =>
        context with
        {
            Result = output,
            Observations = [.. context.Observations, $"{context.DirectionType}: {output}"],

            // On the native path the result is also kept beside the call that asked for it, so
            // the next turn can hand it back as a tool message rather than as narration
            // (SPEC.md §6). Observations still carry it too: they are what the V0 path reads,
            // and what the trace and the Judge read on both.
            ToolExchanges = WithExchange(context, output),

            Status = AgentStatus.Working
        };

    // The host classifies first, because it is accountable for the deployment and can speak for
    // tools it did not write. Then the tool, which is the only thing that knows what it does.
    // Requiring approval still implies Irreversible when nobody said otherwise — that is what it
    // meant before, and a list written against an earlier release keeps its meaning.
    //
    // What is gone is the coupling that made RiskLevel.Sensitive unreachable: risk and approval
    // were one predicate over one collection, so a tool was Irreversible if it needed approval and
    // Safe if it did not, and the middle level named a state the framework could not produce.
    private RiskLevel RiskLevelFor(string toolName)
    {
        if (this.declaredRisk.TryGetValue(toolName, out RiskLevel hostDeclared))
        {
            return hostDeclared;
        }

        if (this.toolRisk.TryGetValue(toolName, out RiskLevel toolDeclared)
            && toolDeclared is not RiskLevel.Safe)
        {
            return toolDeclared;
        }

        return this.irreversibleToolNames.Contains(toolName)
            ? RiskLevel.Irreversible
            : RiskLevel.Safe;
    }

    // What the act is about to touch, as the tool named it. The framework never parses arguments:
    // only the tool knows what its own arguments mean, and a host reinventing that parsing inside
    // a policy delegate is how every deployment ends up with a different, unchecked answer.
    private string ScopeFor(string toolName, string arguments) =>
        this.toolScope.TryGetValue(toolName, out Func<string, string>? scopeOf)
            ? scopeOf(arguments)
            : string.Empty;

    // The mode speaks only for what the explicit permissions did not mention. An allow-list that
    // names the tool has already answered the question — asking anyway would make the list
    // meaningless — and a host policy broker is assumed to have said what it meant, because the
    // framework cannot tell a considered yes from an incidental one.
    private bool AskBecauseNothingPermittedIt(AgentEffect effect) =>
        this.permissionMode is PermissionMode.Ask
            && this.explicitlyPermits?.Invoke(effect) is not true;

    // Deny's twin of the predicate above, sharing its reading of "explicitly permitted" so the
    // two modes cannot drift: an allow-list entry names the act, and a RequireApproval name
    // routes it to an authority instead. Everything else is refused.
    private bool DeniedBecauseNothingPermitsIt(AgentEffect effect) =>
        this.permissionMode is PermissionMode.Deny
            && effect.ApprovalRequired is false
            && this.explicitlyPermits?.Invoke(effect) is not true;

    private bool RequiresApproval(string toolName) =>
        this.irreversibleToolNames.Contains(toolName);

    // Enforcement speaks only where selection spoke: a run with no recorded offering (no
    // selector configured) has nothing to enforce, and a name selection never saw (undescribed,
    // §6.1) is not selection's to withhold.
    private bool DeniedBecauseSelectionWithheldIt(string toolName) =>
        this.enforceSelection
            && AgentRun.Current?.OfferedTools is { } offered
            && this.advertisedToolNames.Contains(toolName)
            && offered.Contains(toolName, StringComparer.OrdinalIgnoreCase) is false;
}
