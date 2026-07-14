// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Direction;

namespace Standard.Agents.Services.Foundations.Direction;

public sealed class ExternalToolService : IExternalToolService
{
    private readonly IMcpBroker mcpBroker;

    public ExternalToolService(IMcpBroker mcpBroker) =>
        this.mcpBroker = mcpBroker;

    public async ValueTask<string> CallAsync(string toolName, string input) =>
        await this.mcpBroker.CallAsync(toolName, input);
}
