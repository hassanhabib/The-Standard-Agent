using Standard.Agents.Models;

namespace Standard.Agents.Services.Orchestrations.Data;

public interface IDataOrchestrationService
{
    ValueTask<AgentContext> RecallAsync(AgentContext context);
}
