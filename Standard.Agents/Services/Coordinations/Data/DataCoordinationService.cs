// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Models.Brokers.Sessions;
using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Services.Orchestrations.Data.Recollections;
using Standard.Agents.Services.Orchestrations.Data.Retrievals;

namespace Standard.Agents.Services.Coordinations.Data;

// The Data nature: two regions, and the composing that belongs to neither of them.
//
// Retrieval brings authored material selected by relevance; Recollection brings what the agent
// accumulated. Deciding that both land in one system prompt and one observation list is this
// tier's job, because it is the only place that can see both.
public partial class DataCoordinationService : IDataCoordinationService
{
    private readonly IRetrievalOrchestrationService retrievalService;
    private readonly IRecollectionOrchestrationService recollectionService;
    private readonly ILoggingBroker loggingBroker;

    public DataCoordinationService(
        IRetrievalOrchestrationService retrievalService,
        IRecollectionOrchestrationService recollectionService,
        ILoggingBroker loggingBroker)
    {
        this.retrievalService = retrievalService;
        this.recollectionService = recollectionService;
        this.loggingBroker = loggingBroker;
    }

    public ValueTask<AgentContext> RecallAsync(AgentContext context) =>
    TryCatch(async () =>
    {
        ValidateContext(context);

        // Announced before anything is fetched, so the trace reads in the order things happened.
        await this.loggingBroker.LogProcessAsync("Data", $"Received prompt: \"{context.Prompt}\"");

        string systemPrompt = await this.retrievalService.RetrieveInstructionsAsync(context.Route);
        IReadOnlyList<string> memories = await this.recollectionService.RecallMemoriesAsync();

        IReadOnlyList<string> knowledge =
            await this.retrievalService.RetrieveGroundingAsync(context.Prompt);

        await this.loggingBroker.LogProcessAsync(
            "Data",
            $"System prompt sent to Decision →{Environment.NewLine}{systemPrompt}",
            detail: true);

        return context with
        {
            SystemPrompt = systemPrompt,
            Observations = [.. context.Observations, .. memories, .. knowledge]
        };
    });

    public ValueTask RememberAsync(string memory) =>
        this.recollectionService.RememberAsync(memory);

    public ValueTask<AgentSession?> RecallSessionAsync(string sessionId) =>
        this.recollectionService.RecallSessionAsync(sessionId);

    public ValueTask RecordSessionAsync(AgentSession session) =>
        this.recollectionService.RecordSessionAsync(session);
}
