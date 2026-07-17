// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Services.Foundations.Direction;

namespace Standard.Agents.Services.Orchestrations.Direction;

// The Direction nature — fan out to the effectors. Invariant 7.8: effects leave
// only via Direction, and external state enters only as Data through it.
//
// Nothing here sets AgentStatus.Failed (decision on #34). Categorical exceptions
// propagate and Coordination maps them — a Failed flag would say "something went
// wrong" where the exception ladder says which faculty failed, how, and whether
// waiting helps.
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
            // A refusal is still an answer — the caller gets told, not stonewalled —
            // so it takes the same terminal path and differs only in its status.
            string result = await this.returnService.ReturnAsync(context.Payload);

            return context with
            {
                Result = result,
                Status = ToTerminalStatus(context.DirectionType)
            };
        }

        bool isLocalTool = await this.internalToolService.HandlesAsync(context.DirectionType);

        // An unknown tool is not an error. Anything the agent cannot do locally may
        // still be doable across the boundary, so a miss routes out (vector 05).
        string output = isLocalTool
            ? await this.internalToolService.RunAsync(context.DirectionType, context.Payload)
            : await this.externalToolService.CallAsync(context.DirectionType, context.Payload);

        // SPEC.md 4.3: a non-terminal result MUST be appended to observations and its
        // status MUST be Working. Named, because a bare "2" tells the Brain nothing
        // about which question it answers once several tools are in flight.
        //
        // Result is written here too, not only on terminal turns. SPEC.md 3 says
        // "result: written by Act" with no qualifier, and vector 06 depends on it: a
        // capped loop never reaches a terminal turn, and SPEC.md 5 still returns
        // context.result. Result holds the most recent thing Direction produced.
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
