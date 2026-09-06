// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Security.Claims;
using Standard.Agents.Models.Orchestrations.Effects;

namespace Standard.Agents.Host.Security;

/// <summary>
/// Who is acting, read from the request's authenticated user and handed to the agent per act
/// (SPEC.md §4.9). Whatever scheme the deployment configured established the user; this only
/// translates the claims the token world uses into the principal the policy decides on and the
/// audit stamps. An anonymous request, or no request at all, resolves to nobody: the framework
/// consumes a principal and never mints one (principal review 2026-09-04, F-10).
/// </summary>
public sealed class HttpPrincipalResolver
{
    private readonly IHttpContextAccessor httpContextAccessor;

    public HttpPrincipalResolver(IHttpContextAccessor httpContextAccessor) =>
        this.httpContextAccessor = httpContextAccessor;

    public AgentPrincipal? Resolve()
    {
        ClaimsPrincipal? user = this.httpContextAccessor.HttpContext?.User;

        if (user?.Identity?.IsAuthenticated is not true)
        {
            return null;
        }

        // "sub" is the token's subject; NameIdentifier is the same thing in .NET's vocabulary
        // after the default inbound mapping; the identity's name is the last resort.
        string? id = user.FindFirst("sub")?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.Identity.Name;

        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return new AgentPrincipal
        {
            Id = id,
            TenantId = user.FindFirst("tid")?.Value ?? user.FindFirst("tenant")?.Value,
            Jurisdiction = user.FindFirst("jurisdiction")?.Value,
            DelegatedBy = user.FindFirst("act")?.Value
        };
    }
}
