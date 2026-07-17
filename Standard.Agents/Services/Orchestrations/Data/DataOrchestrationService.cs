// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Services.Foundations.Data;

namespace Standard.Agents.Services.Orchestrations.Data;

// The Data nature — refresh what the agent HAS. Three foundations, which satisfies
// the Two-Three rule exactly.
public partial class DataOrchestrationService : IDataOrchestrationService
{
    private readonly ISkillService skillService;
    private readonly IMemoryService memoryService;
    private readonly IKnowledgeService knowledgeService;
    private readonly ILoggingBroker loggingBroker;

    public DataOrchestrationService(
        ISkillService skillService,
        IMemoryService memoryService,
        IKnowledgeService knowledgeService,
        ILoggingBroker loggingBroker)
    {
        this.skillService = skillService;
        this.memoryService = memoryService;
        this.knowledgeService = knowledgeService;
        this.loggingBroker = loggingBroker;
    }

    public ValueTask<AgentContext> RecallAsync(AgentContext context) =>
    TryCatch(async () =>
    {
        ValidateContext(context);

        string systemPrompt = await this.skillService.RetrieveSkillsAsync();
        IReadOnlyList<string> memories = await this.memoryService.RecallMemoriesAsync();

        // Appended, never replaced. Recall runs every turn (SPEC.md 5), so replacing
        // observations would wipe the tool results Direction fed back before the
        // Brain ever read them — and vector 02 could not pass.
        return context with
        {
            SystemPrompt = systemPrompt,
            Observations = [.. context.Observations, .. memories]
        };
    });
}
