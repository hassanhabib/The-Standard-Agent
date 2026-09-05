// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Host.Models.V1;

/// <summary>
/// A tool call the caller executed and is answering. The call id is the whole point: the model
/// that asked for the call expects the result back naming that call, and an id the host invents
/// is one the model cannot match.
/// </summary>
public sealed record ToolExchangeV1(
    string CallId,
    string ToolName,
    string ArgumentsJson,
    string Result);
