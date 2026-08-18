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
using Standard.Agents.Services.Foundations.Brains;

namespace Standard.Agents.Services.Orchestrations.Decision.Inferences;

public partial class InferenceOrchestrationService : IInferenceOrchestrationService
{
    private const string ActionPrefix = "ACTION:";
    private const string ToolPrefix = "TOOL:";
    private const string FinalPrefix = "FINAL:";
    private const string ReturnResponseDirection = "ReturnResponse";
    private const string RespondIntent = "Respond";

    private readonly IBrainService brainService;
    private readonly ILoggingBroker loggingBroker;
    private readonly IReadOnlyList<ToolDefinition> toolDefinitions;

    public InferenceOrchestrationService(
        IBrainService brainService,
        ILoggingBroker loggingBroker,
        IReadOnlyList<ToolDefinition>? toolDefinitions = null)
    {
        this.brainService = brainService;
        this.loggingBroker = loggingBroker;
        this.toolDefinitions = toolDefinitions ?? [];
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

        string reply = await this.brainService.GenerateAsync(
            systemPrompt: context.SystemPrompt,
            userPrompt: BuildUserMessage(context));

        return Interpret(context, reply.Trim());
    });

    public async IAsyncEnumerable<AgentStreamEvent> DecideStreamAsync(
        AgentContext context,
        Action<AgentContext> setDecided,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var classifier = new ReplyStreamClassifier();
        var reply = new StringBuilder();
        string userMessage = BuildUserMessage(context);

        IAsyncEnumerable<string> tokens = this.brainService.GenerateStreamAsync(
            systemPrompt: context.SystemPrompt,
            userPrompt: userMessage,
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
            "Decision",
            $"Brain replied →{Environment.NewLine}{reply.ToString().Trim()}",
            detail: true);

        int estimatedTokens =
            (context.SystemPrompt.Length + userMessage.Length + reply.Length) / 4;

        await this.loggingBroker.LogProcessAsync(
            "Decision",
            $"Brain → ~{estimatedTokens} tokens (prompt + reply, estimated)",
            detail: true);

        await this.loggingBroker.LogProcessAsync(
            "Decision", $"Interpreted → {decided.DirectionType}");

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
