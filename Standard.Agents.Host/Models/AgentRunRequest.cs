// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Host.Models;

/// <summary>What to run: the prompt, and nothing else the protocol needs yet.</summary>
public sealed record AgentRunRequest(string Prompt);
