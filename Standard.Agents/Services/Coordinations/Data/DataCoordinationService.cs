// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Models.Brokers.Generators.V1;
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

        // The caller's vocabulary, appended per run (design §6.1). On the text protocol this is
        // the only way the model learns those words exist; on the native path the definitions
        // travel as data too, and both say the same thing for the same reason the tool catalog
        // is rendered in one place.
        systemPrompt = WithCallerVocabulary(systemPrompt, context);

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

    // The same line format the tool catalog uses, under a heading that says who executes: the
    // model may name these words, and the agent never runs one — a call naming a caller tool is
    // a terminal answer addressed to the caller (design §6.2).
    private static string WithCallerVocabulary(string systemPrompt, AgentContext context)
    {
        IReadOnlyList<ToolDefinition> callerTools = context.Inference?.CallerTools ?? [];

        if (callerTools.Count == 0)
        {
            return systemPrompt;
        }

        IEnumerable<string> lines = callerTools.Select(tool =>
            $"- {tool.Name} — {tool.Description} parameters: {tool.ParametersJson}");

        return systemPrompt
            + "\n\nThe caller also accepts these tool calls. Invoke them exactly like tools; "
            + "the caller executes them, not you:\n"
            + string.Join("\n", lines);
    }

    public ValueTask RememberAsync(string memory) =>
        this.recollectionService.RememberAsync(memory);

    public ValueTask<AgentSession?> RecallSessionAsync(string sessionId) =>
        this.recollectionService.RecallSessionAsync(sessionId);

    public ValueTask RecordSessionAsync(AgentSession session) =>
        this.recollectionService.RecordSessionAsync(session);
}
