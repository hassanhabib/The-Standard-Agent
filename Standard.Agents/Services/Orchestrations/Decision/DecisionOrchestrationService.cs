// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Text;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Services.Foundations.Brains;
using Standard.Agents.Services.Foundations.Gates;
using Standard.Agents.Services.Foundations.Judges;

namespace Standard.Agents.Services.Orchestrations.Decision;

public partial class DecisionOrchestrationService : IDecisionOrchestrationService
{
    private const string ActionPrefix = "ACTION:";
    private const string FinalPrefix = "FINAL:";
    private const string RefuseVerdict = "refuse";
    private const string RefuseDirection = "Refuse";
    private const string ReturnResponseDirection = "ReturnResponse";
    private const string RespondIntent = "Respond";

    private const double MinimumAcceptableScore = 0.3;

    private readonly IGateService gateService;
    private readonly IBrainService brainService;
    private readonly IJudgeService judgeService;
    private readonly ILoggingBroker loggingBroker;

    public DecisionOrchestrationService(
        IGateService gateService,
        IBrainService brainService,
        IJudgeService judgeService,
        ILoggingBroker loggingBroker)
    {
        this.gateService = gateService;
        this.brainService = brainService;
        this.judgeService = judgeService;
        this.loggingBroker = loggingBroker;
    }

    public ValueTask<AgentContext> ThinkAsync(AgentContext context) =>
    TryCatch(async () =>
    {
        ValidateContext(context);

        string verdict =
            await this.gateService.ScreenAsync(context.SystemPrompt, context.Prompt);

        if (IsRefusal(verdict))
        {
            return context with
            {
                Intent = RefuseDirection,
                DirectionType = RefuseDirection,
                Payload = "I'm not able to help with that.",
                RawReply = verdict
            };
        }

        string reply =
            (await this.brainService.GenerateAsync(
                systemPrompt: context.SystemPrompt,
                userPrompt: BuildUserMessage(context))).Trim();

        AgentContext decided = Interpret(context, reply);

        bool isFinalAnswer =
            decided.DirectionType.Equals(
                ReturnResponseDirection,
                StringComparison.OrdinalIgnoreCase);

        if (isFinalAnswer is false)
        {
            return decided;
        }

        double score = 
            await this.judgeService.EvaluateAsync(
                judgePrompt: context.SystemPrompt,
                candidate: decided.Payload);

        if (score < MinimumAcceptableScore)
        {
            return context with
            {
                Observations =
                [
                    .. context.Observations,
                    $"A previous draft was rejected on review: {decided.Payload}"
                ],

                Status = AgentStatus.Working
            };
        }

        return decided;
    });

    private static bool IsRefusal(string verdict) =>
verdict.TrimStart().StartsWith(RefuseVerdict, StringComparison.OrdinalIgnoreCase);

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
            firstLine.StartsWith(ActionPrefix, StringComparison.OrdinalIgnoreCase);

        if (modelChoseToAct)
        {
            string[] toolCall =
                firstLine[ActionPrefix.Length..]
                .Split(':', 2, StringSplitOptions.TrimEntries);

            string toolName = toolCall[0];
            string toolInput = toolCall.Length > 1 ? toolCall[1] : string.Empty;

            return context with
            {
                Intent = toolName,
                DirectionType = toolName,
                Payload = toolInput,
                RawReply = reply
            };
        }

        string answer = reply.StartsWith(FinalPrefix, StringComparison.OrdinalIgnoreCase)
            ? reply[FinalPrefix.Length..].Trim()
            : reply;

        return context with
        {
            Intent = RespondIntent,
            DirectionType = ReturnResponseDirection,
            Payload = answer,
            RawReply = reply
        };
    }
        }
