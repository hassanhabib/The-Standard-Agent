// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Reflection;
using FluentAssertions;
using Xunit;

namespace Standard.Agents.Tests.Unit.Clients;

// SPEC.md §4.8: every capability MUST be reachable three ways — Local (in the core, point at a
// resource), External (a provider package, passed as a broker) and Custom (host-authored code).
//
// This is the principle most likely to erode quietly: nothing about adding a capability with one
// mode fails a build, and by the time anyone notices there are six of them. So it is a test
// rather than a convention. A missing mode is a build failure; a mode that genuinely does not
// apply is waived here, in code, with its reason — which makes waiving it a reviewed change
// rather than an omission nobody sees.
public class StandardAgentCapabilityTests
{
    private sealed record Capability(
        string Name,
        string Local,
        string External,
        string Custom,
        string? Waiver = null);

    // The matrix. Every capability the agent exposes, and the method that reaches it each way.
    private static readonly Capability[] capabilities =
    [
        new("Skills", Local: "Skills", External: "UseSkills", Custom: "OnSkills",
            Waiver: "Custom pending: ISkillBroker is the override until a delegate form lands"),

        new("Memory", Local: "Memory", External: "UseMemory", Custom: "OnMemory",
            Waiver: "Custom pending: IMemoryBroker is the override until a delegate form lands"),

        new("Knowledge", Local: "Knowledge", External: "UseKnowledge", Custom: "OnKnowledge",
            Waiver: "Custom pending: IKnowledgeBroker is the override until a delegate form lands"),

        new("Brain", Local: "Brain", External: "UseGenerator", Custom: "OnBrain"),

        new("Gate", Local: "RuleGate", External: "Gate", Custom: "OnGate"),

        new("Judge", Local: "RuleJudge", External: "Judge", Custom: "OnJudge"),

        new("Tools", Local: "Tool", External: "Mcp", Custom: "Tool"),

        new("Trace", Local: "LogTo", External: "UseLogging", Custom: "UseLogging"),

        new("Audit", Local: "Audit", External: "UseAudit", Custom: "OnAudit")
    ];

    private static readonly string[] publicMethodNames =
        [.. typeof(StandardAgent)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(method => method.Name)
            .Distinct()];

    public static TheoryData<string, string, string> UnwaivedModes()
    {
        var modes = new TheoryData<string, string, string>();

        foreach (Capability capability in capabilities)
        {
            modes.Add(capability.Name, "Local", capability.Local);
            modes.Add(capability.Name, "External", capability.External);

            if (capability.Waiver is null)
            {
                modes.Add(capability.Name, "Custom", capability.Custom);
            }
        }

        return modes;
    }

    [Theory]
    [MemberData(nameof(UnwaivedModes))]
    public void ShouldExposeEveryCapabilityLocalExternalAndCustom(
        string capabilityName,
        string mode,
        string expectedMethodName)
    {
        // given, when, then
        publicMethodNames.Should().Contain(
            expectedMethodName,
            because: $"{capabilityName} must be reachable {mode} through .{expectedMethodName}(...) "
                + "— SPEC.md §4.8 makes a capability with a missing mode incomplete");
    }

    // The waiver list is the plan's outstanding debt, and it must reach zero by the release that
    // completes the capability matrix. Pinning the count means retiring a waiver is a deliberate
    // edit here, and adding one to a NEW capability fails immediately rather than quietly.
    [Fact]
    public void ShouldWaiveNoMoreCapabilityModesThanTheMatrixAllows()
    {
        // given
        int expectedWaiverCount = 3;

        // when
        Capability[] actualWaived =
            [.. capabilities.Where(capability => capability.Waiver is not null)];

        // then
        actualWaived.Should().HaveCount(
            expectedWaiverCount,
            because: "every waiver is debt against SPEC.md §4.8 and must be retired, "
                + "and no new capability may ship with one");

        actualWaived.Should().OnlyContain(capability =>
            string.IsNullOrWhiteSpace(capability.Waiver) == false);
    }
}
