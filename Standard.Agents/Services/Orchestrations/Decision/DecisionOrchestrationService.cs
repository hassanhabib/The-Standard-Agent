// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Text;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Services.Foundations.Decision;

namespace Standard.Agents.Services.Orchestrations.Decision;

// The Decision nature — one brain, flanked by conscience. Gate screens the input,
// the Brain reasons, the Judge screens a final answer. It authors no prompt text:
// it frames the task, reads the reply, and interprets it into the next direction.
public partial class DecisionOrchestrationService : IDecisionOrchestrationService
{
    private const string ActionPrefix = "ACTION:";
    private const string FinalPrefix = "FINAL:";
    private const string RefuseVerdict = "refuse";
    private const string RefuseDirection = "Refuse";
    private const string ReturnResponseDirection = "ReturnResponse";
    private const string RespondIntent = "Respond";

    // SPEC.md fixes no threshold. Below this a draft loops instead of returning.
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

        // GATE — the conscience at the front door. A refusal returns here, so the
        // prompt never reaches the Brain at all.
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

        // BRAIN — the one brain reasons.
        string reply =
            (await this.brainService.GenerateAsync(
                context.SystemPrompt,
                BuildUserMessage(context))).Trim();

        AgentContext decided = Interpret(context, reply);

        bool isFinalAnswer =
            decided.DirectionType.Equals(
                ReturnResponseDirection,
                StringComparison.OrdinalIgnoreCase);

        if (isFinalAnswer is false)
        {
            // The Judge screens OUTPUT. A tool call is a proposal, not output.
            return decided;
        }

        // JUDGE — the conscience at the back door. Invariant 7.5: a draft is not a
        // commitment.
        double score =
            await this.judgeService.EvaluateAsync(context.SystemPrompt, decided.Payload);

        if (score < MinimumAcceptableScore)
        {
            // Theory Ch.8: the draft becomes Data and the next Think reconsiders it.
            // The draft itself must travel, not just a note that it failed — the
            // Brain cannot revise what it cannot see.
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

    // StartsWith, not equality. GateService returns the verdict verbatim (#25), so a
    // refusal may carry its reason — "refuse: asks for credentials". An equality
    // check would read that as an allow and let the prompt through.
    private static bool IsRefusal(string verdict) =>
        verdict.TrimStart().StartsWith(RefuseVerdict, StringComparison.OrdinalIgnoreCase);

    // Frames the task; authors no rules. Invariant 7.2 — the instructions are the
    // system prompt, and that came from Data.
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
        // First line only. SPEC.md 6 makes this a MUST — models emit extra lines, and
        // honouring a stray second line would run the wrong branch (vector 03).
        string firstLine = reply.Split('\n')[0].Trim();

        bool modelChoseToAct =
            firstLine.StartsWith(ActionPrefix, StringComparison.OrdinalIgnoreCase);

        if (modelChoseToAct)
        {
            // Split on the FIRST colon only, so a colon inside the input survives —
            // "https://example.com:8080" must reach the tool whole.
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

        // A FINAL may span lines, so the prefix is stripped from the WHOLE reply
        // rather than from the first line (vector 04). A reply with no prefix is
        // still an answer — a model that forgot four characters should not lose its
        // work.
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
