// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Services.Foundations.ExternalTools;
using Standard.Agents.Services.Foundations.InternalTools;
using Standard.Agents.Services.Foundations.Returns;

namespace Standard.Agents.Services.Orchestrations.Direction;

public partial class DirectionOrchestrationService : IDirectionOrchestrationService
{
    private const string ReturnResponseDirection = "ReturnResponse";
    private const string RefuseDirection = "Refuse";

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
    TryCatch(async () =>
    {
        ValidateContext(context);

        if (IsTerminal(context.DirectionType))
        {
            string result = await this.returnService.ReturnAsync(context.Payload);

            return context with
            {
                Result = result,
                Status = ToTerminalStatus(context.DirectionType)
            };
        }

        bool isLocalTool = await this.internalToolService.HandlesAsync(context.DirectionType);

        string output = isLocalTool
? await this.internalToolService.RunAsync(context.DirectionType, context.Payload)
: await this.externalToolService.CallAsync(context.DirectionType, context.Payload);

        return context with
        {
            Result = output,
            Observations = [.. context.Observations, $"{context.DirectionType}: {output}"],
            Status = AgentStatus.Working
        };
    });

    private static bool IsTerminal(string directionType) =>
        directionType.Equals(ReturnResponseDirection, StringComparison.OrdinalIgnoreCase)
            || directionType.Equals(RefuseDirection, StringComparison.OrdinalIgnoreCase);

    private static AgentStatus ToTerminalStatus(string directionType) =>
        directionType.Equals(RefuseDirection, StringComparison.OrdinalIgnoreCase)
            ? AgentStatus.Refused
            : AgentStatus.Responded;
        }
