// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Models.Brokers.Mcps;

/// <summary>
/// One tool an MCP server offers, as its catalog describes it — the name a call must use, the
/// description that would advertise it, and the JSON Schema of the arguments it takes. What a
/// tool takes is part of what advertises it; a server that declares no schema takes an open
/// object.
/// </summary>
public sealed record McpTool(string Name, string Description, string InputSchemaJson = "{}");
