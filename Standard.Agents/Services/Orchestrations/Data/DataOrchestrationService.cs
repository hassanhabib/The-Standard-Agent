using Standard.Agents.Models;
using Standard.Agents.Services.Foundations.Data;

namespace Standard.Agents.Services.Orchestrations.Data;

// DATA — Recall. Coordinates the three Data foundations (Skills, Memory, Knowledge)
// and folds what the agent HAS into the context. Skills become the system prompt;
// recalled memory + retrieved knowledge seed the working observations.
public sealed class DataOrchestrationService : IDataOrchestrationService
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

    public async ValueTask<AgentContext> RecallAsync(AgentContext context)
    {
        string skills = await this.skillService.RetrieveSkillsAsync();
        IReadOnlyList<string> memories = await this.memoryService.RecallMemoriesAsync();
        IReadOnlyList<string> knowledge = await this.knowledgeService.RetrieveKnowledgeAsync(context.Prompt);

        IReadOnlyList<string> recalled = [.. memories, .. knowledge];

        return context with
        {
            SystemPrompt = skills,
            Observations = context.Observations.Count == 0 && recalled.Count > 0
                ? recalled
                : context.Observations
        };
    }
}
