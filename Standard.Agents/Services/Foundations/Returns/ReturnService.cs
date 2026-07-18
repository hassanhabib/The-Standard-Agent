// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Loggings;

namespace Standard.Agents.Services.Foundations.Returns;

public partial class ReturnService : IReturnService
{
    private readonly ILoggingBroker loggingBroker;

    public ReturnService(ILoggingBroker loggingBroker) =>
        this.loggingBroker = loggingBroker;

    public ValueTask<string> ReturnAsync(string payload) =>
    TryCatch(() =>
    {
        ValidatePayload(payload);

        return ValueTask.FromResult(payload);
    });
}
