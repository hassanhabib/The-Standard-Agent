// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Orchestrations.Effects;

namespace Standard.Agents.Brokers.Policies;

// The Local mode: least privilege as a list of names, which is what .AllowTools() has always
// meant. It becomes a policy broker so that the simple answer and an external policy engine
// travel the same seam, and so a denial carries a reason from the very first release.
public sealed class AllowListPolicyBroker : IPolicyBroker
{
    private readonly HashSet<string> allowedToolNames;

    public AllowListPolicyBroker(IEnumerable<string> allowedToolNames) =>
        this.allowedToolNames =
            new HashSet<string>(allowedToolNames, StringComparer.OrdinalIgnoreCase);

    public async ValueTask<AuthorizationDecision> AuthorizeAsync(AgentEffect effect)
    {
        await Task.CompletedTask;

        return this.allowedToolNames.Contains(effect.ToolName)
            ? AuthorizationDecision.Allow()
            : AuthorizationDecision.Deny(
                $"tool '{effect.ToolName}' is not permitted");
    }
}
