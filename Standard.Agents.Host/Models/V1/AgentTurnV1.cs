// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Host.Models.V1;

/// <summary>One past exchange in the caller-owned transcript: what was asked, what was answered.</summary>
public sealed record AgentTurnV1(string Prompt, string Answer);
