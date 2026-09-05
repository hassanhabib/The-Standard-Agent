// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Models.Brokers.Mcps;

internal sealed record ToolListResult(
    IReadOnlyList<ToolListEntry> Tools);

// The schema rides as the raw element it arrived as: the broker hands it on as text and never
// interprets it, so a server's own vocabulary reaches the model unchanged.
internal sealed record ToolListEntry(
    string Name,
    string? Description,
    System.Text.Json.JsonElement? InputSchema);

internal sealed record JsonRpcToolListResponse(
    ToolListResult? Result,
    JsonRpcError? Error);
