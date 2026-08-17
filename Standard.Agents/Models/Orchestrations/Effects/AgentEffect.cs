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
        AgentPrincipal? principal = null) =>
        new()
        {
            RunId = runId,

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

        return Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
}
