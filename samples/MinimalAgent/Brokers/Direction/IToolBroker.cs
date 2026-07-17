// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace MinimalAgent.Brokers.Direction;

public interface IToolBroker
{
    bool Has(string name);

    ValueTask<string> RunAsync(string name, string input);
}
