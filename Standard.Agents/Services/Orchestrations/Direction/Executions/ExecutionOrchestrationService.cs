// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Services.Foundations.ExternalTools;
using Standard.Agents.Services.Foundations.InternalTools;
using Standard.Agents.Services.Foundations.Returns;

namespace Standard.Agents.Services.Orchestrations.Direction.Executions;

public partial class ExecutionOrchestrationService : IExecutionOrchestrationService
{
    private readonly IInternalToolService internalToolService;
    private readonly IExternalToolService externalToolService;
    private readonly IReturnService returnService;
    private readonly ILoggingBroker loggingBroker;

    public ExecutionOrchestrationService(
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

    public ValueTask<bool> HandlesLocallyAsync(string toolName) =>
        this.internalToolService.HandlesAsync(toolName);

    // Local first, then across the boundary. A name the agent owns is never sent outward.
    public ValueTask<string> RunAsync(string toolName, string payload) =>
    TryCatch(async () =>
    {
        bool isLocalTool = await this.internalToolService.HandlesAsync(toolName);

        return isLocalTool
            ? await this.internalToolService.RunAsync(toolName, payload)
            : await this.externalToolService.CallAsync(toolName, payload);
    });

    public ValueTask<string> ReturnAsync(string payload) =>
        this.returnService.ReturnAsync(payload);

    public ValueTask<string> CompensateAsync(string toolName, string arguments, string outcome) =>
    TryCatch(async () =>
    {
        if (await this.internalToolService.HandlesAsync(toolName) is false)
        {
            // An external act cannot be undone from here, and saying so is the honest answer.
            // Reporting it as reversed would be worse than reporting nothing.
            return string.Empty;
        }

        return await this.internalToolService.CompensateAsync(toolName, arguments, outcome);
    });
}
