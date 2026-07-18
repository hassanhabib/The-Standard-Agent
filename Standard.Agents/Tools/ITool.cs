// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Tools;

public interface ITool
{
    string Name { get; }

    string Description => string.Empty;

    string Parameters => "{}";

    ValueTask<string> ExecuteAsync(string input);
}
