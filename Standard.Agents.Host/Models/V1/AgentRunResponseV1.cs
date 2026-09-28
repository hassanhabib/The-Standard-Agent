// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Host.Models.V1;

/// <summary>
/// How the run ended, in protocol form: the wire form of <c>AgentOutcome</c>, version 1.
/// Status travels beside the result because only <c>Responded</c> makes the result an answer,
/// and the pending effect travels beside them because a stateless caller that cannot see the
/// act the run is waiting on cannot perform it, approve it, or answer it. The failure travels
/// beside them because a run that stopped without an answer says why as a code, and a caller
/// that can only read the sentence cannot decide whether to retry. Null on every run that
/// answered, so a caller that never reads it sees the response it always saw.
/// </summary>
public sealed record AgentRunResponseV1(
    string Result,
    string Status,
    PendingEffectV1? PendingEffect,
    FailureV1? Failure = null);
