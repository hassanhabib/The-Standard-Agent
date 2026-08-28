// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Text;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Models.Brokers.Generators.V1;
using Standard.Agents.Models.Clients.Agents;
using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Models.Foundations.Usages;
using Standard.Agents.Services.Foundations.Brains;
using Standard.Agents.Services.Foundations.Usages;

namespace Standard.Agents.Services.Orchestrations.Decision.Inferences;

public partial class InferenceOrchestrationService : IInferenceOrchestrationService
{
    private const string ActionPrefix = "ACTION:";
    private const string ToolPrefix = "TOOL:";
    private const string FinalPrefix = "FINAL:";
    private const string TransferPrefix = "TRANSFER:";
    private const string SayPrefix = "SAY:";
    private const string ReturnResponseDirection = "ReturnResponse";
    private const string RespondIntent = "Respond";

    // The task a transfer hands over when the model names only the agent. The grounded handoff
    // template already carries the user's actual ask ({prompt}), so the task slot states the
    // transfer's meaning rather than repeating the prompt into its own context.
    private const string TransferTask = "answer the user's request in full.";

    private readonly IBrainService brainService;
    private readonly IUsageService usageService;
    private readonly ILoggingBroker loggingBroker;
    private readonly IReadOnlyList<ToolDefinition> toolDefinitions;

    public InferenceOrchestrationService(
        IBrainService brainService,
        IUsageService usageService,
        ILoggingBroker loggingBroker,
        IReadOnlyList<ToolDefinition>? toolDefinitions = null)
    {
        this.brainService = brainService;
        this.usageService = usageService;
        this.loggingBroker = loggingBroker;
        this.toolDefinitions = toolDefinitions ?? [];
    }

    // The provider's own report wins whenever there is one — it is what the invoice will be
    // drawn from. Counting is the fallback, and it says which it was, because a bound enforced
    // on an estimate and a bound reconciled against a bill are different claims.
    //
    // Without this, a text-protocol run reported zero tokens every turn and every budget it was
    // given silently did nothing.
    private async ValueTask<AgentContext> MeasuredAsync(
        AgentContext decided,
        string sent,
        string received)
    {
        if (decided.PromptTokens > 0 || decided.CompletionTokens > 0)
        {
            return decided;
        }

        AgentUsage usage = await this.usageService.MeasureAsync(sent, received);

        return decided with
        {
            PromptTokens = usage.PromptTokens,
            CompletionTokens = usage.CompletionTokens,
            UsageIsEstimated = usage.IsEstimated
        };
    }

    public ValueTask<AgentContext> DecideAsync(AgentContext context) =>
    TryCatch(async () =>
    {
        // Which protocol answers is decided here and nowhere else. Adopting native tool calling
        // changes how a choice is READ, not what the agent is (SPEC.md §6).
        if (this.brainService.SpeaksNatively)
        {
            return await ThinkNativelyAsync(context);
        }

        string userMessage = BuildUserMessage(context);

        // The context carries the resolution's output from the boundary; this tier hands it on
        // and learns nothing from it (docs/per-request-inference.md §2). A context built by hand
        // carries none, and that is exactly the plain call.
        string reply = context.Inference is null
            ? await this.brainService.GenerateAsync(
                systemPrompt: context.SystemPrompt,
                userPrompt: userMessage)
            : await this.brainService.GenerateAsync(
                systemPrompt: context.SystemPrompt,
                userPrompt: userMessage,
                inference: context.Inference);

        AgentContext decided = await MeasuredAsync(
            Interpret(context, reply.Trim()),
            sent: context.SystemPrompt + userMessage,
            received: reply);

        await NarrateDecidedAsync(decided, reply.Trim());

        return decided;
    });

    // One narration for the text protocol's model call, whichever door drove it. The streamed
    // door alone used to narrate the reply, the token count and the interpretation — so a
    // batched run's trace never showed what its own model calls cost, and no trace-comparing
    // check could hold the doors to each other.
    private async ValueTask NarrateDecidedAsync(AgentContext decided, string reply)
    {
        await this.loggingBroker.LogProcessAsync(
            "Decision",
            $"Brain replied →{Environment.NewLine}{reply}",
            detail: true);

        await this.loggingBroker.LogProcessAsync(
            "Decision",
            $"Brain → {decided.PromptTokens + decided.CompletionTokens} tokens "
                + $"({(decided.UsageIsEstimated ? "counted" : "reported")})",
            detail: true);

        await this.loggingBroker.LogProcessAsync(
            "Decision", $"Interpreted → {decided.DirectionType}");
    }

    // The streamed door, mapped like the batched one: a fault in the model stream surfaces in
    // the same orchestration family DecideAsync's TryCatch produces. The enumeration advances
    // inside the catch and yields outside it — an iterator cannot wrap a yield, but the failure
    // points are the awaits.
    public async IAsyncEnumerable<AgentStreamEvent> DecideStreamAsync(
        AgentContext context,
        Action<AgentContext> setDecided,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using IAsyncEnumerator<AgentStreamEvent> segments =
            DecideStreamCoreAsync(context, setDecided, cancellationToken)
                .GetAsyncEnumerator(cancellationToken);

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

    private async IAsyncEnumerable<AgentStreamEvent> DecideStreamCoreAsync(
        AgentContext context,
        Action<AgentContext> setDecided,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // The same seam DecideAsync keeps: which protocol answers is decided here and nowhere
        // else (SPEC.md §6). This branch was missing, so a native-brain agent asked to stream
        // fell through to the V0 text seam — the placeholder that throws. A V1 provider returns
        // one structured result rather than a token stream, so the draft arrives whole and is
        // surfaced as Thinking: a draft is not an answer until the guardians settle it, exactly
        // as the text path's chunks are not.
        if (this.brainService.SpeaksNatively)
        {
            AgentContext nativeDecided = await ThinkNativelyAsync(context);

            bool isDraftAnswer =
                nativeDecided.DirectionType.Equals(
                    ReturnResponseDirection, StringComparison.OrdinalIgnoreCase)
                        && string.IsNullOrWhiteSpace(nativeDecided.Payload) is false;

            if (isDraftAnswer)
            {
                yield return new AgentStreamEvent(
                    AgentStreamEventType.Thinking, nativeDecided.Payload);
            }

            setDecided(nativeDecided);

            yield break;
        }

        var classifier = new ReplyStreamClassifier();
        var reply = new StringBuilder();
        string userMessage = BuildUserMessage(context);

        // The same hand-off the batched call makes: the streamed loop carries the resolution's
        // output too, or the stream becomes the way to step around the seam (SPEC.md §7.6).
        IAsyncEnumerable<string> tokens = context.Inference is null
            ? this.brainService.GenerateStreamAsync(
                systemPrompt: context.SystemPrompt,
                userPrompt: userMessage,
                cancellationToken: cancellationToken)
            : this.brainService.GenerateStreamAsync(
                systemPrompt: context.SystemPrompt,
                userPrompt: userMessage,
                inference: context.Inference,
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

        AgentContext decided = await MeasuredAsync(
            Interpret(context, reply.ToString().Trim()),
            sent: context.SystemPrompt + userMessage,
            received: reply.ToString());

        await NarrateDecidedAsync(decided, reply.ToString().Trim());

        setDecided(decided);
    }

    // A draft is not an answer until the Judge has settled it, so what streams out while the
    // model is still talking is Thinking rather than Response. Filtering a stream to Response
    // therefore equals what the batched call returns (SPEC.md §4.3).
    private static AgentStreamEvent AsUnsettledDraft(AgentStreamEvent segment) =>
        segment.Type is AgentStreamEventType.Response
            ? segment with { Type = AgentStreamEventType.Thinking }
            : segment;
}
