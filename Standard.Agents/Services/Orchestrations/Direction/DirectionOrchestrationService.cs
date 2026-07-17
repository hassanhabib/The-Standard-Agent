// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Services.Foundations.Direction;

namespace Standard.Agents.Services.Orchestrations.Direction;

public partial class DirectionOrchestrationService : IDirectionOrchestrationService
{
    private readonly IInternalToolService internalToolService;
    private readonly IExternalToolService externalToolService;
    private readonly IReturnService returnService;
    private readonly ILoggingBroker loggingBroker;

    public DirectionOrchestrationService(
        IInternalToolService internalToolService,
        IExternalToolService externalToolService,
        IReturnService returnService,
        ILoggingBroker loggingBroker)
    {
        this.internalToolService = internalToolService;
        this.externalToolService = externalToolService;
        this.returnService = returnService;
        this.loggingBroker = loggingBroker;
    }

    public ValueTask<AgentContext> ActAsync(AgentContext context) =>
        throw new NotImplementedException();
}
