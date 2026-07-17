// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Classifiers;
using Standard.Agents.Brokers.Loggings;

namespace Standard.Agents.Services.Foundations.Gates;

public partial class GateService : IGateService
{
    private readonly IClassifierBroker classifierBroker;
    private readonly ILoggingBroker loggingBroker;

    public GateService(
        IClassifierBroker classifierBroker,
        ILoggingBroker loggingBroker)
    {
        this.classifierBroker = classifierBroker;
        this.loggingBroker = loggingBroker;
    }

    public ValueTask<string> ScreenAsync(string input) =>
    TryCatch(async () =>
    {
        ValidateScreen(input);

        return await this.classifierBroker.ClassifyAsync(input);
    });
    }
