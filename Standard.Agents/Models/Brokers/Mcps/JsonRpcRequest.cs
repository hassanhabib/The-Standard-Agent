// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Text.Json.Serialization;

namespace Standard.Agents.Models.Brokers.Mcps;

// Params is null for parameterless methods such as tools/list, and the serializer omits it
// (WhenWritingNull) rather than sending "params": null, which some servers reject.
internal sealed record JsonRpcRequest(
    [property: JsonPropertyName("jsonrpc")] string JsonRpc,
    int Id,
    string Method,
    ToolCallParams? Params);
