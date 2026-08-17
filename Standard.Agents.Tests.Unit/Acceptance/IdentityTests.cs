// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Brokers.Knowledges;
using Standard.Agents.Brokers.Memorys;
using Standard.Agents.Brokers.Skills;
using Standard.Agents.Models.Foundations.Skills;
using Standard.Agents.Models.Orchestrations.Effects;
using Standard.Agents.Tools;
using Xunit;

namespace Standard.Agents.Tests.Unit.Acceptance;

// Identity-aware authorization (SPEC.md §4.9). The Enterprise profile claims it, and the claim is
// only true if the principal reaches the decision — not merely the record of it. A policy engine
// asked "may this act happen?" without being told whose act it is can answer nothing about
// identity, however faithfully the audit log names the caller afterwards.
public class IdentityTests
{
    private sealed class CountingTool : ITool
    {
        public string Name => "wire_transfer";
        public string Description => "Moves money.";
        public string Parameters => "{}";

        public int ExecutionCount { get; private set; }

        public ValueTask<string> ExecuteAsync(string input)
        {
            ExecutionCount++;

            return ValueTask.FromResult("transfer complete");
        }
    }

    private static StandardAgent AgentThatCallsTheTool(CountingTool tool, params string[] replies)
    {
        var skillBroker = new Mock<ISkillBroker>();
        skillBroker.Setup(broker => broker.SelectSkillsAsync()).ReturnsAsync(new List<Skill>());

        var memoryBroker = new Mock<IMemoryBroker>();
        memoryBroker.Setup(broker => broker.SelectMemoriesAsync()).ReturnsAsync([]);

        var knowledgeBroker = new Mock<IKnowledgeBroker>();

        knowledgeBroker.Setup(broker => broker.SelectKnowledgeAsync(It.IsAny<string>()))
            .ReturnsAsync([]);

        var scriptedReplies = new Queue<string>(replies);

        return new StandardAgent()
            .UseSkills(skillBroker.Object)
            .UseMemory(memoryBroker.Object)
            .UseKnowledge(knowledgeBroker.Object)
            .Tool(tool)
            .OnBrain(async (systemPrompt, userPrompt) =>
                scriptedReplies.Count > 0 ? scriptedReplies.Dequeue() : "FINAL: done");
    }

    [Fact]
    public async Task ShouldCarryThePrincipalIntoTheAuthorizationDecisionAsync()
    {
        // given
        var tool = new CountingTool();
        string? seenByPolicy = null;

        StandardAgent agent =
            AgentThatCallsTheTool(tool, "ACTION: wire_transfer: 10000", "FINAL: paid")
                .Principal(() => "teller-42")
                .OnPolicy(effect =>
                {
                    seenByPolicy = effect.Principal;

                    return ValueTask.FromResult(AuthorizationDecision.Allow());
                });

        // when
        await agent.ProcessPromptAsync(prompt: "pay the invoice");

        // then — an audit trail that names the caller after the fact is not authorization
        seenByPolicy.Should().Be("teller-42");
    }

    [Fact]
    public async Task ShouldLetPolicyRefuseAnActOnTheIdentityAloneAsync()
    {
        // given — the same act, permitted for one principal and not the other
        var tool = new CountingTool();
        string secondPrompt = string.Empty;
        int brainCalls = 0;

        var skillBroker = new Mock<ISkillBroker>();
        skillBroker.Setup(broker => broker.SelectSkillsAsync()).ReturnsAsync(new List<Skill>());

        var memoryBroker = new Mock<IMemoryBroker>();
        memoryBroker.Setup(broker => broker.SelectMemoriesAsync()).ReturnsAsync([]);

        var knowledgeBroker = new Mock<IKnowledgeBroker>();

        knowledgeBroker.Setup(broker => broker.SelectKnowledgeAsync(It.IsAny<string>()))
            .ReturnsAsync([]);

        StandardAgent agent = new StandardAgent()
            .UseSkills(skillBroker.Object)
            .UseMemory(memoryBroker.Object)
            .UseKnowledge(knowledgeBroker.Object)
            .Tool(tool)
            .Principal(() => "intern-9")
            .OnBrain(async (systemPrompt, userPrompt) =>
            {
                brainCalls++;

                if (brainCalls > 1)
                {
                    secondPrompt = userPrompt;
                }

                return brainCalls == 1
                    ? "ACTION: wire_transfer: 10000"
                    : "FINAL: I could not do that";
            })
            .OnPolicy(effect => ValueTask.FromResult(
                effect.Principal is "teller-42"
                    ? AuthorizationDecision.Allow()
                    : AuthorizationDecision.Deny(
                        $"'{effect.Principal}' may not move money")));

        // when
        string actualResult = await agent.ProcessPromptAsync(prompt: "pay the invoice");

        // then — the denial names who was refused, which is what makes it reviewable
        tool.ExecutionCount.Should().Be(0);
        secondPrompt.Should().Contain("'intern-9' may not move money");
        actualResult.Should().Be("I could not do that");
    }

    // An identifier alone answers "who" and nothing else. A policy engine is routinely written as
    // "this principal, in this tenant, under this jurisdiction" — and a delegated act, where a
    // service acts on a person's behalf, is a different question from that service acting for
    // itself (SPEC.md §4.9).
    [Fact]
    public async Task ShouldCarryTheWholeIdentityIntoTheAuthorizationDecisionAsync()
    {
        // given
        var tool = new CountingTool();
        AgentPrincipal? seenByPolicy = null;

        StandardAgent agent =
            AgentThatCallsTheTool(tool, "ACTION: wire_transfer: 10000", "FINAL: paid")
                .Principal(() => new AgentPrincipal
                {
                    Id = "svc-payments",
                    TenantId = "acme-eu",
                    Jurisdiction = "EU",
                    DelegatedBy = "teller-42"
                })
                .OnPolicy(effect =>
                {
                    seenByPolicy = effect.Identity;

                    return ValueTask.FromResult(AuthorizationDecision.Allow());
                });

        // when
        await agent.ProcessPromptAsync(prompt: "pay the invoice");

        // then
        seenByPolicy.Should().NotBeNull();
        seenByPolicy!.Id.Should().Be("svc-payments");
        seenByPolicy.TenantId.Should().Be("acme-eu");
        seenByPolicy.Jurisdiction.Should().Be("EU");
        seenByPolicy.DelegatedBy.Should().Be("teller-42");
    }

    [Fact]
    public async Task ShouldRefuseAnActOnJurisdictionAloneAsync()
    {
        // given — the same act, by the same principal, permitted in one regime and not another
        var tool = new CountingTool();

        StandardAgent agent =
            AgentThatCallsTheTool(
                tool, "ACTION: wire_transfer: 10000", "FINAL: I could not do that")
                .Principal(() => new AgentPrincipal { Id = "teller-42", Jurisdiction = "EU" })
                .OnPolicy(effect => ValueTask.FromResult(
                    effect.Identity?.Jurisdiction is "US"
                        ? AuthorizationDecision.Allow()
                        : AuthorizationDecision.Deny("cross-border transfers need US booking")));

        // when
        await agent.ProcessPromptAsync(prompt: "pay the invoice");

        // then
        tool.ExecutionCount.Should().Be(0);
    }

    // The id-only overload still works and still reads the same way, so nothing written against
    // 1.0 has to change to keep authorizing.
    [Fact]
    public async Task ShouldKeepTheIdentifierOnlyPrincipalWorkingAsync()
    {
        // given
        var tool = new CountingTool();
        string? seenId = null;
        AgentPrincipal? seenIdentity = null;

        StandardAgent agent =
            AgentThatCallsTheTool(tool, "ACTION: wire_transfer: 10000", "FINAL: paid")
                .Principal(() => "teller-42")
                .OnPolicy(effect =>
                {
                    seenId = effect.Principal;
                    seenIdentity = effect.Identity;

                    return ValueTask.FromResult(AuthorizationDecision.Allow());
                });

        // when
        await agent.ProcessPromptAsync(prompt: "pay the invoice");

        // then — the id reads as before, and the richer view carries the same identity
        seenId.Should().Be("teller-42");
        seenIdentity!.Id.Should().Be("teller-42");
        seenIdentity.TenantId.Should().BeNull();
    }

    [Fact]
    public async Task ShouldCarryNoPrincipalWhenNoneIsConfiguredAsync()
    {
        // given — the neutral case: nothing configured, nothing asserted about identity
        var tool = new CountingTool();
        bool policyRan = false;
        string? seenByPolicy = "not asked";

        StandardAgent agent =
            AgentThatCallsTheTool(tool, "ACTION: wire_transfer: 10000", "FINAL: paid")
                .OnPolicy(effect =>
                {
                    policyRan = true;
                    seenByPolicy = effect.Principal;

                    return ValueTask.FromResult(AuthorizationDecision.Allow());
                });

        // when
        await agent.ProcessPromptAsync(prompt: "pay the invoice");

        // then — absent an identity the effect claims none, rather than inventing one
        policyRan.Should().BeTrue();
        seenByPolicy.Should().BeNull();
    }
}
