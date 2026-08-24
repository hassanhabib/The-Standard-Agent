// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Models.Orchestrations.Effects;

namespace Standard.Agents.Models.Coordinations.Directions;

/// <summary>
/// The perimeter's standing orders, as one datum: what mode permission runs in, what each tool
/// declared about itself, and the host's own hooks for who is acting and what is explicitly
/// permitted. Policy is Data — these ride together as a value the perimeter consults, not as
/// constructor sprawl no dependency count can see.
/// </summary>
public sealed record PerimeterPolicy
{
    private static readonly IReadOnlyDictionary<string, RiskLevel> noRisk =
        new Dictionary<string, RiskLevel>(StringComparer.OrdinalIgnoreCase);

    private static readonly IReadOnlyDictionary<string, Func<string, string>> noScope =
        new Dictionary<string, Func<string, string>>(StringComparer.OrdinalIgnoreCase);

    /// <summary>What permission means when nothing named the act (SPEC.md §4.9).</summary>
    public PermissionMode Mode { get; init; } = PermissionMode.Open;

    /// <summary>The tools whose acts require approval before running.</summary>
    public IReadOnlyCollection<string> IrreversibleTools { get; init; } = [];

    /// <summary>Risk the host declared per tool, overriding what the tool says of itself.</summary>
    public IReadOnlyDictionary<string, RiskLevel> DeclaredRisk { get; init; } = noRisk;

    /// <summary>Risk each tool declares about itself, read once at composition.</summary>
    public IReadOnlyDictionary<string, RiskLevel> ToolRisk { get; init; } = noRisk;

    /// <summary>
    /// Each tool's own reading of what an act touches (ITool.ScopeOf). The tool is the only
    /// thing that knows what its arguments mean; the framework never parses them.
    /// </summary>
    public IReadOnlyDictionary<string, Func<string, string>> ToolScope { get; init; } = noScope;

    /// <summary>
    /// Whether the configured allow-list speaks to an act at all — which the mode needs and a
    /// yes/no authorization decision cannot carry. Null when no allow-list was configured, so
    /// Ask asks about everything.
    /// </summary>
    public Func<AgentEffect, bool>? ExplicitlyPermits { get; init; }

    /// <summary>
    /// Who is acting — asked per act rather than captured at composition, because the principal
    /// can change between prompts on a shared agent (SPEC.md §4.4).
    /// </summary>
    public Func<AgentPrincipal?>? IdentityResolver { get; init; }
}
