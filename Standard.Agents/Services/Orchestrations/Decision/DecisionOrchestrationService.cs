// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Services.Foundations.Decision;

namespace Standard.Agents.Services.Orchestrations.Decision;

// The Decision nature — one brain, flanked by conscience. Gate screens the input,
// the Brain reasons, the Judge screens a final answer. It authors no prompt text:
// it frames the task, reads the reply, and interprets it into the next direction.
public partial class DecisionOrchestrationService : IDecisionOrchestrationService
{
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
        throw new NotImplementedException();
}
