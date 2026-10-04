// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Text.Json.Nodes;

namespace Standard.Agents.Models.Brokers.Mcps;

internal sealed record InitializeParams(
    string ProtocolVersion,
    JsonObject Capabilities,
    ClientInfo ClientInfo);

internal sealed record ClientInfo(
    string Name,
    string Version);
