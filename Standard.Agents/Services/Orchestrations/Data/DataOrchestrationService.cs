// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Services.Foundations.Knowledges;
using Standard.Agents.Services.Foundations.Memorys;
using Standard.Agents.Services.Foundations.Skills;

namespace Standard.Agents.Services.Orchestrations.Data;

public partial class DataOrchestrationService : IDataOrchestrationService
{
    private const string ToolsMarker = "{{tools}}";

    private readonly ISkillService skillService;
    private readonly IMemoryService memoryService;
    private readonly IKnowledgeService knowledgeService;
    private readonly string toolCatalog;
    private readonly ILoggingBroker loggingBroker;

    public DataOrchestrationService(
        ISkillService skillService,
        IMemoryService memoryService,
        IKnowledgeService knowledgeService,
        string toolCatalog,
        ILoggingBroker loggingBroker)
    {
        this.skillService = skillService;
        this.memoryService = memoryService;
        this.knowledgeService = knowledgeService;
        this.toolCatalog = toolCatalog;
        this.loggingBroker = loggingBroker;
    }

    public ValueTask<AgentContext> RecallAsync(AgentContext context) =>
    TryCatch(async () =>
    {
        ValidateContext(context);

        string skills = await this.skillService.RetrieveSkillsAsync();
        IReadOnlyList<string> memories = await this.memoryService.RecallMemoriesAsync();
        IReadOnlyList<string> knowledge = await this.knowledgeService.RetrieveKnowledgeAsync(context.Prompt);

        string systemPrompt = skills.Replace(ToolsMarker, this.toolCatalog);

        await this.loggingBroker.LogProcessAsync("Data", $"Received prompt: \"{context.Prompt}\"");
        await this.loggingBroker.LogProcessAsync("Data", $"Retrieved skills ({skills.Length} chars)", detail: true);
        await this.loggingBroker.LogProcessAsync("Data", $"Recalled {memories.Count} memories");
        await this.loggingBroker.LogProcessAsync("Data", $"Retrieved {knowledge.Count} knowledge matches");
        await this.loggingBroker.LogProcessAsync("Data", $"Assembled system prompt ({systemPrompt.Length} chars)", detail: true);

        return context with
        {
            SystemPrompt = systemPrompt,
            Observations = [.. context.Observations, .. memories, .. knowledge]
        };
    });

    public ValueTask RememberAsync(string memory) =>
        this.memoryService.RememberAsync(memory);
}
