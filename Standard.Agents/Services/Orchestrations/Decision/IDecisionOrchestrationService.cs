using Standard.Agents.Models;

namespace Standard.Agents.Services.Orchestrations.Decision;

public interface IDecisionOrchestrationService
{
    ValueTask<AgentContext> ThinkAsync(AgentContext context);
}
