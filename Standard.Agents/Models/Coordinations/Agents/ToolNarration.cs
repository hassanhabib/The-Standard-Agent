// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Models.Coordinations.Agents;

/// <summary>
/// A tool's declared narration templates: <paramref name="Starting"/> is voiced before the act
/// (supports <c>{tool}</c> and <c>{payload}</c> slots; a model-authored narration overrides it),
/// <paramref name="Observed"/> after the result (supports <c>{tool}</c>; never overridden).
/// Derived from the tool, so the loop can voice acts without holding the tools themselves.
/// </summary>
public sealed record ToolNarration(string Starting, string Observed);
