// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;

namespace Standard.Agents.Models.Orchestrations.Effects;

/// <summary>
/// A proposed act, described. Direction routes a bare name and payload until a perimeter control
/// is configured; then the act needs an identity, because authorization, approval and run-once
/// are all judgments about an act, and an act with no identity cannot be judged the same way
/// twice (SPEC.md §3.3, §4.9).
/// </summary>
public sealed record AgentEffect
{
    private const string KeySeparator = "|";

    public string RunId { get; init; } = "";

    /// <summary>Who is acting, as an identifier. The same value as <c>Identity?.Id</c>.</summary>
    public string? Principal { get; init; }

    /// <summary>
    /// Who is acting, in full — tenant, jurisdiction and delegation where the host knows them
    /// (SPEC.md §4.9). Null when no identity is configured.
    /// </summary>
    public AgentPrincipal? Identity { get; init; }

    public string ToolName { get; init; } = "";
    public string Arguments { get; init; } = "";

    /// <summary>
    /// The model-issued call id, when this effect is a caller's tool call being handed back
    /// (docs/per-request-inference.md §6.2). An exposed protocol requires the result to answer
    /// the id the model minted; an id the exposer invents is one the caller cannot match.
    /// Empty for every other kind of effect, and on the text protocol, which has no ids.
    /// </summary>
    public string CallId { get; init; } = "";

    /// <summary>
    /// What the act is about to touch — a path, a host, an account — as the tool named it
    /// (ITool.ScopeOf). Empty when the tool touches nothing addressable. Permission is not only
    /// what but where, and a policy that can only see a tool name cannot express the difference.
    /// </summary>
    public string Scope { get; init; } = "";

    public RiskLevel RiskLevel { get; init; }
    public bool ApprovalRequired { get; init; }

    /// <summary>
    /// Identifies this act, so a repeat of the same intent is recognisable as the same act.
    /// <b>Derived, never supplied</b> — see <see cref="For"/>.
    /// </summary>
    public string IdempotencyKey { get; init; } = "";

    /// <summary>
    /// Describes an act and derives its key from what the act <i>is</i>: the run, the tool, and a
    /// canonical form of the arguments.
    /// </summary>
    /// <remarks>
    /// The key is deliberately not a parameter. SPEC.md §4.9: a key the caller or the Brain can
    /// choose is a key the model can vary, and run-once degrades to advisory — the model retries
    /// with a nudged argument and the payment goes out twice. Deriving it means the same intent
    /// produces the same key by construction rather than by anyone remembering to.
    /// </remarks>
    public static AgentEffect For(
        string runId,
        string toolName,
        string arguments,
        RiskLevel riskLevel = RiskLevel.Safe,
        bool approvalRequired = false,
        AgentPrincipal? principal = null,
        string scope = "") =>
        new()
        {
            RunId = runId,
            Scope = scope,

            // Both views of one identity, kept in step here so they cannot disagree. The
            // identifier stays for the callers that only ever needed "who".
            Principal = principal?.Id,
            Identity = principal,
            ToolName = toolName,
            Arguments = arguments,
            RiskLevel = riskLevel,
            ApprovalRequired = approvalRequired,
            IdempotencyKey = DeriveKey(runId, toolName, arguments)
        };

    // Whitespace- and case-insensitive on the tool name, so "calculator: 2 + 2" and
    // "calculator:2+2" are one act rather than two. Anything more clever belongs to the tool,
    // which is the only thing that knows what its arguments mean.
    private static string DeriveKey(string runId, string toolName, string arguments)
    {
        string canonicalArguments = string.Join(
            separator: " ",
            arguments.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        string canonical = string.Join(
            KeySeparator,
            runId,
            toolName.Trim().ToLowerInvariant(),
            canonicalArguments);

        // Convert.ToHexStringLower is .NET 9+. Both forms emit identical lowercase hex, which
        // is load-bearing: a key derived on one target must match the same key on the other.
#if NET9_0_OR_GREATER
        return Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
#else
        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
#endif
    }
}
