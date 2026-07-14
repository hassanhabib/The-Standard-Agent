using Standard.Agents.Models;
using Standard.Agents.Services.Foundations.Direction;

namespace Standard.Agents.Services.Orchestrations.Direction;

// DIRECTION — Act. Routes the decision to a locus: Return (up to caller, terminal),
// Internal (a local tool), or External (out across the boundary). A tool result is
// sent back to Data as an observation, keeping the loop going.
public sealed class DirectionOrchestrationService : IDirectionOrchestrationService
{
    private readonly IInternalToolService internalToolService;
    private readonly IExternalToolService externalToolService;
    private readonly IReturnService returnService;

    public DirectionOrchestrationService(
        IInternalToolService internalToolService,
        IExternalToolService externalToolService,
        IReturnService returnService)
    {
        this.internalToolService = internalToolService;
        this.externalToolService = externalToolService;
        this.returnService = returnService;
    }

    public async ValueTask<AgentContext> ActAsync(AgentContext context)
    {
        // RETURN — up to the caller (terminal).
        if (context.DirectionType.Equals("ReturnResponse", StringComparison.OrdinalIgnoreCase))
        {
            string answer = await this.returnService.ReturnAsync(context.Payload);
            return context with { Result = answer, Status = AgentStatus.Responded };
        }

        if (context.DirectionType.Equals("Refuse", StringComparison.OrdinalIgnoreCase))
        {
            string answer = await this.returnService.ReturnAsync(context.Payload);
            return context with { Result = answer, Status = AgentStatus.Refused };
        }

        // INTERNAL — a local tool the agent owns.
        if (this.internalToolService.Handles(context.DirectionType))
        {
            string result = await this.internalToolService.RunAsync(context.DirectionType, context.Payload);

            return context with
            {
                Result = result,
                Observations = [.. context.Observations, $"{context.DirectionType}({context.Payload}) => {result}"],
                Status = AgentStatus.Working
            };
        }

        // EXTERNAL — out across the boundary (MCP / remote). Stub today.
        string external = await this.externalToolService.CallAsync(context.DirectionType, context.Payload);

        return context with
        {
            Result = external,
            Observations = [.. context.Observations, $"{context.DirectionType}({context.Payload}) => {external}"],
            Status = AgentStatus.Working
        };
    }
}
