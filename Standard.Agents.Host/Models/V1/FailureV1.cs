// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Host.Models.V1;

/// <summary>
/// Why the run stopped without an answer, in protocol form: the wire form of
/// <c>AgentFailure</c>, version 1. The code is what a caller switches on — cancelled,
/// budget_exhausted, turns_exhausted, going_in_circles — so a caller across HTTP decides whether
/// to retry the same way a caller in process does, never by reading the sentence.
/// </summary>
public sealed record FailureV1(
    string Category,
    string Code,
    string Message);
