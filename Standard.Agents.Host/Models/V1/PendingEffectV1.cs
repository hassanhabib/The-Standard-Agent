// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Host.Models.V1;

/// <summary>
/// The act the run is waiting on: a caller's tool call to execute and answer, or an act held
/// for an authority's decision. The call id is the model's own, so the caller can answer it;
/// the idempotency key identifies the act, so a decision names exactly one act.
/// </summary>
public sealed record PendingEffectV1(
    string RunId,
    string CallId,
    string ToolName,
    string Arguments,
    string Scope,
    string RiskLevel,
    bool ApprovalRequired,
    string IdempotencyKey,
    string? Principal);
