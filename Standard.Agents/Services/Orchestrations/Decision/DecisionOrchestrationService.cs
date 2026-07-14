using System.Text;
using Standard.Agents.Models;
using Standard.Agents.Services.Foundations.Decision;

namespace Standard.Agents.Services.Orchestrations.Decision;

// DECISION — Think. Coordinates the conscience-wrapped Brain: Gate screens the input,
// the one Brain reasons, the Judge screens the output. It authors no prompt text
// (that is Data); it frames the task, reads the reply, and interprets it into the
// next direction.
public sealed class DecisionOrchestrationService : IDecisionOrchestrationService
{
    private readonly IGateService gateService;
    private readonly IBrainService brainService;
    private readonly IJudgeService judgeService;

    public DecisionOrchestrationService(
        IGateService gateService,
        IBrainService brainService,
        IJudgeService judgeService)
    {
        this.gateService = gateService;
        this.brainService = brainService;
        this.judgeService = judgeService;
    }

    public async ValueTask<AgentContext> ThinkAsync(AgentContext context)
    {
        // GATE — the conscience at the front door.
        string verdict = await this.gateService.ScreenAsync(context.SystemPrompt, context.Prompt);

        if (verdict.Equals("refuse", StringComparison.OrdinalIgnoreCase))
        {
            return context with
            {
                Intent = "Refused",
                DirectionType = "Refuse",
                Payload = "I'm not able to help with that.",
                RawReply = verdict
            };
        }

        // BRAIN — the one brain reasons.
        string userMessage = BuildUserMessage(context);
        string reply =
            (await this.brainService.GenerateAsync(context.SystemPrompt, userMessage)).Trim();

        AgentContext decided = Interpret(context, reply);

        // JUDGE — reflective check on a final answer (the stub verifier approves
        // everything today; a low score would loop instead of returning).
        if (decided.DirectionType.Equals("ReturnResponse", StringComparison.OrdinalIgnoreCase))
        {
            double score = await this.judgeService.EvaluateAsync(context.SystemPrompt, decided.Payload);

            if (score < 0.3)
            {
                return context with
                {
                    Observations = [.. context.Observations, "draft rejected by judge — revise"],
                    Status = AgentStatus.Working
                };
            }
        }

        return decided;
    }

    private static string BuildUserMessage(AgentContext context)
    {
        StringBuilder userMessage = new();

        userMessage.Append("Task: ").Append(context.Prompt);

        if (context.Observations.Count > 0)
        {
            userMessage.AppendLine().AppendLine().AppendLine("Observations so far:");

            foreach (string observation in context.Observations)
            {
                userMessage.Append("- ").AppendLine(observation);
            }
        }

        return userMessage.ToString();
    }

    private static AgentContext Interpret(AgentContext context, string reply)
    {
        string firstLine = reply.Split('\n')[0].Trim();

        bool modelChoseToAct =
            firstLine.StartsWith("ACTION:", StringComparison.OrdinalIgnoreCase);

        if (modelChoseToAct)
        {
            string[] toolCall =
                firstLine["ACTION:".Length..].Split(':', 2, StringSplitOptions.TrimEntries);

            string toolName = toolCall[0];
            string toolInput = toolCall.Length > 1 ? toolCall[1] : string.Empty;

            return context with
            {
                Intent = "UseTool",
                DirectionType = toolName,
                Payload = toolInput,
                RawReply = reply
            };
        }

        string answer = reply.StartsWith("FINAL:", StringComparison.OrdinalIgnoreCase)
            ? reply["FINAL:".Length..].Trim()
            : reply;

        return context with
        {
            Intent = "Answer",
            DirectionType = "ReturnResponse",
            Payload = answer,
            RawReply = reply
        };
    }
}
