// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models;
using Standard.Agents.Services.Foundations.Direction;

namespace Standard.Agents.Services.Orchestrations.Direction;

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

        string external = await this.externalToolService.CallAsync(context.DirectionType, context.Payload);

        return context with
        {
            Result = external,
            Observations = [.. context.Observations, $"{context.DirectionType}({context.Payload}) => {external}"],
            Status = AgentStatus.Working
        };
    }
}
