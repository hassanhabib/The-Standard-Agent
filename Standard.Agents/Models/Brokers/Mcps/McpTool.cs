// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Models.Brokers.Mcps;

/// <summary>
/// One tool an MCP server offers, as its catalog describes it — the name a call must use, and
/// the description that would advertise it.
/// </summary>
public sealed record McpTool(string Name, string Description);
