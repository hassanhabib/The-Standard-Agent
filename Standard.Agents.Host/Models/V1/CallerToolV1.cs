// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Host.Models.V1;

/// <summary>
/// A tool the CALLER will execute, declared so the model may name it. The agent never runs one:
/// a returned call naming it ends the run waiting on the caller, with the call as the pending
/// effect. Parameters are a JSON Schema, as the model reads them.
/// </summary>
public sealed record CallerToolV1(string Name, string Description, string ParametersJson);
