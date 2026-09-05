// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Models.Brokers.Mcps;

// Arguments are whatever JSON object the caller shaped — a native call's own arguments, or
// plain text wrapped upstream — carried as a node so the broker never interprets them.
internal sealed record ToolCallParams(
    string Name,
    System.Text.Json.Nodes.JsonNode? Arguments);
