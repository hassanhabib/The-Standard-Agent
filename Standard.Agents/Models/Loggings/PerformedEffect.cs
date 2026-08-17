// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Models.Loggings;

/// <summary>
/// An act this run actually performed, and what it produced. Recorded so the run can be unwound
/// (SPEC.md §4.9): compensation needs the arguments the tool was called with <i>and</i> the outcome
/// it returned, because the outcome carries the identity the undo has to name.
/// </summary>
/// <remarks>
/// Only performed acts are kept. An effect denied by policy, held for approval, or replayed from
/// the ledger was never performed by this run, and compensating it would undo something this run
/// did not do.
/// </remarks>
public sealed record PerformedEffect(string ToolName, string Arguments, string Outcome);
