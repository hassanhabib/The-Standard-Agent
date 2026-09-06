// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Standard.Agents.Host.Security;
using Standard.Agents.Models.Orchestrations.Effects;
using Xunit;

namespace Standard.Agents.Tests.Unit.Controllers;

// Found in the 2026-09-04 principal review (F-10): the host's API key proved possession of one
// secret and established nobody. Identity now comes from the request's authenticated user, which
// whatever scheme the deployment configured established, and is handed to the agent per act as
// the principal the policy decides on and the audit stamps (SPEC.md §4.9). The wire still has
// no field in which a caller can claim to be someone.
public class HttpPrincipalResolverTests
{
    private static HttpPrincipalResolver ResolverFor(ClaimsPrincipal? user)
    {
        var accessor = new HttpContextAccessor();

        if (user is not null)
        {
            accessor.HttpContext = new DefaultHttpContext { User = user };
        }

        return new HttpPrincipalResolver(accessor);
    }

    [Fact]
    public void ShouldResolveTheAuthenticatedUserAsThePrincipal()
    {
        // given — the claims a bearer token carries, in the names the token world uses
        var identity = new ClaimsIdentity(
            [
                new Claim("sub", "hassan"),
                new Claim("tid", "peerllm"),
                new Claim("jurisdiction", "US-WA"),
                new Claim("act", "ops-bot")
            ],
            authenticationType: "Bearer");

        HttpPrincipalResolver resolver = ResolverFor(new ClaimsPrincipal(identity));

        var expectedPrincipal = new AgentPrincipal
        {
            Id = "hassan",
            TenantId = "peerllm",
            Jurisdiction = "US-WA",
            DelegatedBy = "ops-bot"
        };

        // when
        AgentPrincipal? actualPrincipal = resolver.Resolve();

        // then
        actualPrincipal.Should().BeEquivalentTo(expectedPrincipal);
    }

    [Fact]
    public void ShouldResolveTheNameIdentifierWhenThereIsNoSubjectClaim()
    {
        // given — an identity in the .NET claim vocabulary rather than the token one
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "hassan@peerllm.com")],
            authenticationType: "Cookies");

        HttpPrincipalResolver resolver = ResolverFor(new ClaimsPrincipal(identity));
        var expectedPrincipal = new AgentPrincipal { Id = "hassan@peerllm.com" };

        // when
        AgentPrincipal? actualPrincipal = resolver.Resolve();

        // then
        actualPrincipal.Should().BeEquivalentTo(expectedPrincipal);
    }

    [Fact]
    public void ShouldResolveNoPrincipalWhenTheUserIsNotAuthenticated()
    {
        // given — an anonymous request, which is what an open laptop host receives
        HttpPrincipalResolver resolver =
            ResolverFor(new ClaimsPrincipal(new ClaimsIdentity()));

        // when
        AgentPrincipal? actualPrincipal = resolver.Resolve();

        // then — the framework consumes a principal and never mints one
        actualPrincipal.Should().BeNull();
    }

    [Fact]
    public void ShouldResolveNoPrincipalOutsideARequest()
    {
        // given — no HTTP context at all, as when the agent runs from a background job
        HttpPrincipalResolver resolver = ResolverFor(user: null);

        // when
        AgentPrincipal? actualPrincipal = resolver.Resolve();

        // then
        actualPrincipal.Should().BeNull();
    }
}
