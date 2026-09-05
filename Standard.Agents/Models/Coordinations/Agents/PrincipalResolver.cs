// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Orchestrations.Effects;

namespace Standard.Agents.Models.Coordinations.Agents;

/// <summary>
/// Who the host says is acting (SPEC.md §4.9), asked per run. A model carrying a delegate
/// rather than a naked delegate parameter, because configuration crosses natures where a
/// dependency may not — the same shape the tool selector takes. The loop reads it to stamp a
/// session with its owner and to refuse a session another principal opened (SPEC.md §4.11).
/// </summary>
public sealed class PrincipalResolver
{
    private readonly Func<AgentPrincipal?> resolve;

    public PrincipalResolver(Func<AgentPrincipal?> resolve) =>
        this.resolve = resolve;

    public AgentPrincipal? Resolve() =>
        this.resolve();
}
