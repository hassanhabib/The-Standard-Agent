// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Standard.Agents.Host.Security;
using Xunit;

namespace Standard.Agents.Tests.Unit.Controllers;

// When a deployment configures authentication, an agent route wants an authenticated user and
// the heartbeat stays open; when it configures none, the door is as open as before.
public class IdentityGateTests
{
    [Theory]
    [InlineData(false, false, "/api/agents/runs", true)]
    [InlineData(false, false, "/api/V1/agents/runs", true)]
    [InlineData(true, true, "/api/V1/agents/runs", true)]
    [InlineData(true, false, "/api/V1/agents/runs", false)]
    [InlineData(true, false, "/api/agents/streams", false)]
    [InlineData(true, false, "/api/home", true)]
    [InlineData(true, false, "/API/HOME", true)]
    public void ShouldAllowOnlyAuthenticatedUsersOnAgentRoutesWhenAuthenticationIsConfigured(
        bool authenticationConfigured,
        bool userIsAuthenticated,
        string path,
        bool expectedAllowed)
    {
        // when
        bool actualAllowed = IdentityGate.Allows(authenticationConfigured, userIsAuthenticated, path);

        // then
        actualAllowed.Should().Be(expectedAllowed);
    }
}
