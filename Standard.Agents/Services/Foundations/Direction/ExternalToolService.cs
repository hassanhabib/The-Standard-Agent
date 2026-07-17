// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Direction;
using Standard.Agents.Brokers.Loggings;

namespace Standard.Agents.Services.Foundations.Direction;

// Act outward, across the boundary. Invariant 7.8: external state enters only as
// Data via a Direction that reached out, and effects leave only via Direction.
public partial class ExternalToolService : IExternalToolService
{
    private readonly IMcpBroker mcpBroker;
    private readonly ILoggingBroker loggingBroker;

    public ExternalToolService(
        IMcpBroker mcpBroker,
        ILoggingBroker loggingBroker)
    {
        this.mcpBroker = mcpBroker;
        this.loggingBroker = loggingBroker;
    }

    public ValueTask<string> CallAsync(string name, string input) =>
    TryCatch(async () =>
    {
        ValidateName(name);

        return await this.mcpBroker.CallAsync(name, input);
    });
}
