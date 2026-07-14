using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Models;
using Standard.Agents.Services.Orchestrations.Data;
using Standard.Agents.Services.Orchestrations.Decision;
using Standard.Agents.Services.Orchestrations.Direction;

namespace Standard.Agents.Services.Coordinations;

// The Agent: pure composition. It binds the three orchestrations in a loop, flowing
// one AgentContext through Recall → Think → Act until a terminal Status. It holds no
// nature logic — the loop, and observing the loop (the flow log), is all it does.
public sealed class AgentCoordinationService : IAgentCoordinationService
{
    private const int MaxTurns = 7;

    private readonly IDataOrchestrationService dataOrchestration;
    private readonly IDecisionOrchestrationService decisionOrchestration;
    private readonly IDirectionOrchestrationService directionOrchestration;
    private readonly ILogBroker logBroker;

    public AgentCoordinationService(
        IDataOrchestrationService dataOrchestration,
        IDecisionOrchestrationService decisionOrchestration,
        IDirectionOrchestrationService directionOrchestration,
        ILogBroker logBroker)
    {
        this.dataOrchestration = dataOrchestration;
        this.decisionOrchestration = decisionOrchestration;
        this.directionOrchestration = directionOrchestration;
        this.logBroker = logBroker;
    }

    public async ValueTask<string> ProcessPromptAsync(string prompt)
    {
        await this.logBroker.ResetAsync();
        await this.logBroker.WriteAsync($"RECEIVED: {prompt}");

        AgentContext context = new() { Prompt = prompt };

        for (int turn = 1; turn <= MaxTurns; turn++)
        {
            context = await this.dataOrchestration.RecallAsync(context);
            context = await this.decisionOrchestration.ThinkAsync(context);
            context = await this.directionOrchestration.ActAsync(context);

            await LogTurnAsync(turn, context);

            if (context.Status != AgentStatus.Working)
            {
                break;
            }
        }

        return context.Result;
    }

    private async ValueTask LogTurnAsync(int turn, AgentContext context)
    {
        string observations = context.Observations.Count == 0
            ? "(none)"
            : Environment.NewLine +
              string.Join(Environment.NewLine, context.Observations.Select(o => "  - " + o));

        await this.logBroker.WriteAsync(
            $"{Environment.NewLine}================ turn {turn} ================{Environment.NewLine}" +
            $"[RECALL] observations: {observations}{Environment.NewLine}" +
            $"[THINK]  model said: {context.RawReply}{Environment.NewLine}" +
            $"  parsed -> intent={context.Intent}, direction={context.DirectionType}, " +
            $"payload=\"{context.Payload}\"{Environment.NewLine}" +
            $"[ACT]    status={context.Status}, result=\"{context.Result}\"");
    }
}
