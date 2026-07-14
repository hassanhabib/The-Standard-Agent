using Standard.Agents.Models;

namespace Standard.Agents.Services.Orchestrations.Direction;

public interface IDirectionOrchestrationService
{
    ValueTask<AgentContext> ActAsync(AgentContext context);
}
