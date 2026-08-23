// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Orchestrations.Effects;

namespace Standard.Agents.Brokers.Policies;

// The Local mode: least privilege as a list, which is what .AllowTools() has always meant. It is
// a policy broker so that the simple answer and an external policy engine travel the same seam,
// and so a denial carries a reason from the very first release.
//
// An entry says WHAT and, optionally, WHERE:
//
//   "search"                 — the tool, wherever it points
//   "write_file:/project"    — the tool, only where the scope starts with /project
//
// The second form exists because "may write files" is not "may write files under /project", and
// an agent with hands is defined by that difference. The scope is whatever the tool named through
// ITool.ScopeOf — the framework never parses arguments, because only the tool knows what they
// mean.
//
// Matching is a prefix, deliberately, and that boundary is stated rather than discovered: no
// globs, no regular expressions, no path canonicalisation. A host that needs those supplies a
// real policy engine through .UsePolicy, and one that needs "/project" not to match
// "/project-secrets" should say "/project/".
public sealed class AllowListPolicyBroker : IPolicyBroker
{
    private readonly Dictionary<string, List<string>> allowedScopesByTool;

    public AllowListPolicyBroker(IEnumerable<string> allowedEntries)
    {
        this.allowedScopesByTool = new Dictionary<string, List<string>>(
            StringComparer.OrdinalIgnoreCase);

        foreach (string entry in allowedEntries)
        {
            int separator = entry.IndexOf(':');

            string toolName = separator < 0 ? entry : entry[..separator];
            string scope = separator < 0 ? string.Empty : entry[(separator + 1)..];

            if (this.allowedScopesByTool.TryGetValue(toolName, out List<string>? scopes) is false)
            {
                scopes = [];
                this.allowedScopesByTool[toolName] = scopes;
            }

            scopes.Add(scope);
        }
    }

    public async ValueTask<AuthorizationDecision> AuthorizeAsync(AgentEffect effect)
    {
        await Task.CompletedTask;

        if (this.allowedScopesByTool.TryGetValue(effect.ToolName, out List<string>? allowedScopes)
            is false)
        {
            return AuthorizationDecision.Deny($"tool '{effect.ToolName}' is not permitted");
        }

        // An entry with no scope permits the tool anywhere, which is what every entry meant
        // before scopes existed — so a list written against an earlier release keeps its meaning.
        bool permitted = allowedScopes.Any(allowed =>
            allowed.Length is 0
                || effect.Scope.StartsWith(allowed, StringComparison.OrdinalIgnoreCase));

        return permitted
            ? AuthorizationDecision.Allow()
            : AuthorizationDecision.Deny(
                $"tool '{effect.ToolName}' is not permitted at '{effect.Scope}'");
    }

    // Whether anything in the list speaks to this act at all. The permission MODE needs to know
    // the difference between "explicitly permitted" and "nobody mentioned it" — a distinction a
    // yes/no decision cannot carry.
    public bool Mentions(AgentEffect effect) =>
        this.allowedScopesByTool.ContainsKey(effect.ToolName);
}
