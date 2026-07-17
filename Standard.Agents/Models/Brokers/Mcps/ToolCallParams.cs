// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Models.Brokers.Mcps;

internal sealed record ToolCallParams(
    string Name,
    IReadOnlyDictionary<string, string> Arguments);
