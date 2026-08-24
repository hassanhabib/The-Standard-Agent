// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Standard.Agents.Host.Security;
using Xunit;

namespace Standard.Agents.Tests.Unit.Controllers;

public class ApiKeyGateTests
{
    [Fact]
    public void ShouldAllowEverythingWhenNoKeyIsConfigured()
    {
        // given . when
        bool actualDecision = ApiKeyGate.Allows(
            configuredKey: null,
            presentedKey: null,
            path: "/api/agents/runs");

        // then — an unset key is a deliberate open door, not a locked one with no key cut.
        actualDecision.Should().BeTrue();
    }

    [Fact]
    public void ShouldAllowTheAgentRoutesOnlyWithTheConfiguredKey()
    {
        // given . when
        bool matchingDecision = ApiKeyGate.Allows(
            configuredKey: "psk-live-7",
            presentedKey: "psk-live-7",
            path: "/api/agents/runs");

        bool mismatchedDecision = ApiKeyGate.Allows(
            configuredKey: "psk-live-7",
            presentedKey: "psk-live-8",
            path: "/api/agents/runs");

        bool absentDecision = ApiKeyGate.Allows(
            configuredKey: "psk-live-7",
            presentedKey: null,
            path: "/api/agents/streams");

        // then
        matchingDecision.Should().BeTrue();
        mismatchedDecision.Should().BeFalse();
        absentDecision.Should().BeFalse();
    }

    [Fact]
    public void ShouldKeepTheHeartbeatOpenForProbes()
    {
        // given . when — a load balancer cannot present a key, and the heartbeat tells it
        // nothing an attacker could use.
        bool actualDecision = ApiKeyGate.Allows(
            configuredKey: "psk-live-7",
            presentedKey: null,
            path: "/");

        // then
        actualDecision.Should().BeTrue();
    }
}
