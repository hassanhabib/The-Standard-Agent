// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Tools;

namespace Standard.Agents.Conformance;

public sealed class StubTool : ITool
{
    private readonly string output;

    public string Name { get; }

    public StubTool(string name, string output)
    {
        this.Name = name;
        this.output = output;
    }

    public ValueTask<string> ExecuteAsync(string input) =>
        ValueTask.FromResult(this.output);
    }
