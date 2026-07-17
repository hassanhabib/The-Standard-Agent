// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Brokers.Generators;

public interface IGeneratorBroker
{
    ValueTask<string> GenerateAsync(string systemPrompt, string userPrompt);
}
