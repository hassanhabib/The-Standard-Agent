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

        string systemPrompt = await this.skillService.RetrieveSkillsAsync();
        IReadOnlyList<string> memories = await this.memoryService.RecallMemoriesAsync();

        return context with
        {
            SystemPrompt = systemPrompt,
            Observations = [.. context.Observations, .. memories]
        };
    });
}
