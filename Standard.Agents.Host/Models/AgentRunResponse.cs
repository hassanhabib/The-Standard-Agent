// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Host.Models;

/// <summary>
/// How the run ended, in protocol form. Status travels beside the result because only
/// <c>Responded</c> makes the result an answer — a consumer that cannot tell a held run
/// from an answered one will eventually report unfinished work as done.
/// </summary>
public sealed record AgentRunResponse(string Result, string Status);
