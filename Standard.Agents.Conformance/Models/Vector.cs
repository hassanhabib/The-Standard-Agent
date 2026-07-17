// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Conformance;

public sealed record Vector(
    string Name,
    string? Description,
    List<string> GeneratorReplies,
    Dictionary<string, string>? Tools,
    string Prompt,
    Expectation Expect);
