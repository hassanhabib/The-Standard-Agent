// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Models.Orchestrations.Effects;

/// <summary>
/// What became of one effect when the run was unwound (SPEC.md §4.9).
/// </summary>
/// <param name="ToolName">The tool whose act was being undone.</param>
/// <param name="Undone">
/// Whether it was actually reversed. <c>false</c> means the effect stands — either the tool
/// declared no way back, or the attempt to undo it failed.
/// </param>
/// <param name="Detail">What was done, or why it could not be.</param>
public sealed record CompensationOutcome(string ToolName, bool Undone, string Detail);
