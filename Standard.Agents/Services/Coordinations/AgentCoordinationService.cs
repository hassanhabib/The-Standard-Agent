// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Services.Orchestrations.Data;
using Standard.Agents.Services.Orchestrations.Decision;
using Standard.Agents.Services.Orchestrations.Direction;

namespace Standard.Agents.Services.Coordinations;

public partial class AgentCoordinationService : IAgentCoordinationService
{
    private readonly IDataOrchestrationService dataOrchestrationService;
    private readonly IDecisionOrchestrationService decisionOrchestrationService;
    private readonly IDirectionOrchestrationService directionOrchestrationService;
    private readonly ILogBroker logBroker;
    private readonly ILoggingBroker loggingBroker;

    public AgentCoordinationService(
        IDataOrchestrationService dataOrchestrationService,
        IDecisionOrchestrationService decisionOrchestrationService,
        IDirectionOrchestrationService directionOrchestrationService,
        ILogBroker logBroker,
        ILoggingBroker loggingBroker)
    {
        this.dataOrchestrationService = dataOrchestrationService;
        this.decisionOrchestrationService = decisionOrchestrationService;
        this.directionOrchestrationService = directionOrchestrationService;
        this.logBroker = logBroker;
        this.loggingBroker = loggingBroker;
    }

    public ValueTask<string> ProcessPromptAsync(string prompt) =>
        throw new NotImplementedException();
}
