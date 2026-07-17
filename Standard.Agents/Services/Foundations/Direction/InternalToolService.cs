// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Direction;
using Standard.Agents.Brokers.Loggings;

namespace Standard.Agents.Services.Foundations.Direction;

public partial class InternalToolService : IInternalToolService
{
    private readonly IToolBroker toolBroker;
    private readonly ILoggingBroker loggingBroker;

    public InternalToolService(
        IToolBroker toolBroker,
        ILoggingBroker loggingBroker)
    {
        this.toolBroker = toolBroker;
        this.loggingBroker = loggingBroker;
    }

    public ValueTask<bool> HandlesAsync(string name) =>
    TryCatch(async () =>
    {
        ValidateName(name);

        return await this.toolBroker.HasAsync(name);
    });

    // The tool's output is returned verbatim, whatever it says. A tool reporting
    // an error is a result, not a failure — it becomes an observation and the
    // loop carries on. Only a throwing tool is a dependency failure.
    public ValueTask<string> RunAsync(string name, string input) =>
    TryCatch(async () =>
    {
        ValidateName(name);

        return await this.toolBroker.RunAsync(name, input);
    });
}
