// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Models.Brokers.Mcps;

internal sealed record ToolListResult(
    IReadOnlyList<ToolListEntry> Tools);

internal sealed record ToolListEntry(
    string Name,
    string? Description);

internal sealed record JsonRpcToolListResponse(
    ToolListResult? Result,
    JsonRpcError? Error);
