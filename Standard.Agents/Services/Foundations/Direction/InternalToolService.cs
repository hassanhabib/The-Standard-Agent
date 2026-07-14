// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Direction;

namespace Standard.Agents.Services.Foundations.Direction;

public sealed class InternalToolService : IInternalToolService
{
    private readonly IToolBroker toolBroker;

    public InternalToolService(IToolBroker toolBroker) =>
        this.toolBroker = toolBroker;

    public bool Handles(string toolName) =>
        this.toolBroker.Has(toolName);

    public async ValueTask<string> RunAsync(string toolName, string input) =>
        await this.toolBroker.RunAsync(toolName, input);
}
