// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

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

    public DataOrchestrationService(
        ISkillService skillService,
        IMemoryService memoryService,
        IKnowledgeService knowledgeService)
    {
        this.skillService = skillService;
        this.memoryService = memoryService;
        this.knowledgeService = knowledgeService;
    }

    public ValueTask<AgentContext> RecallAsync(AgentContext context) =>
        throw new NotImplementedException();
}
