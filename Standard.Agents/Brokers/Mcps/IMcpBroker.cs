// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Brokers.Mcps;

public interface IMcpBroker
{
    ValueTask<string> CallAsync(string name, string input);
}
