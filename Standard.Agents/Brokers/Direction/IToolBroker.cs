// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Brokers.Direction;

public interface IToolBroker
{
    ValueTask<bool> HasAsync(string name);

    ValueTask<string> RunAsync(string name, string input);
}
