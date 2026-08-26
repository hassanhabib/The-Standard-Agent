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
        string? Local,
        string External,
        string Custom,
        string? Waiver = null);

    // The matrix. Every capability the agent exposes, and the method that reaches it each way.
    private static readonly Capability[] capabilities =
    [
        new("Skills", Local: "Skills", External: "UseSkills", Custom: "OnSkills"),

        new("Memory", Local: "Memory", External: "UseMemory", Custom: "OnMemory"),

        new("Knowledge", Local: "Knowledge", External: "UseKnowledge", Custom: "OnKnowledge"),

        // Brain has no Local mode, and this is the one documented N/A in the matrix. Local means
        // "in the core, no dependency" (SPEC.md §4.8) — and running a model in-process
        // inherently needs an inference runtime, which cannot live in a dependency-free core.
        // The External mode covers it: a GGUF through the LlamaSharp package is one line. This
        // is an omission with a reason, which §4.8 permits; silence would not be.
        new("Brain", Local: null, External: "UseGenerator", Custom: "OnBrain",
            Waiver: "Local N/A: in-process inference needs a runtime, and the core is "
                + "dependency-free by design. External covers it (LlamaSharp)."),

        new("NativeBrain", Local: null, External: "UseNativeBrain", Custom: "OnNativeBrain",
            Waiver: "Local N/A: same reason as Brain — native tool calling still needs a model."),

        new("Gate", Local: "RuleGate", External: "Gate", Custom: "OnGate"),

        new("Judge", Local: "RuleJudge", External: "Judge", Custom: "OnJudge"),

        new("Tools", Local: "Tool", External: "Mcp", Custom: "Tool"),

        new("Trace", Local: "LogTo", External: "UseLogging", Custom: "UseLogging"),

        new("Audit", Local: "Audit", External: "UseAudit", Custom: "OnAudit"),

        new("Policy", Local: "AllowTools", External: "UsePolicy", Custom: "OnPolicy"),

        new("Approval", Local: "RequireApproval", External: "UseApprovals", Custom: "OnApproval"),

        new("Resilience", Local: "Resilience", External: "UseResilience", Custom: "Fallback"),

        new("Sessions", Local: "Sessions", External: "UseSessions", Custom: "OnSessions"),

        new("EffectLedger", Local: "EffectLedger", External: "UseEffectLedger",
            Custom: "OnEffectLedger"),

        new("Usage", Local: "Usage", External: "UseUsage", Custom: "OnUsage"),

        // Observability the collector's way: spans and metrics beside the trace's prose and the
        // audit's records. Local is the in-box ActivitySource; Custom is a delegate for
        // pipelines no ActivityListener reaches.
        new("Telemetry", Local: "Telemetry", External: "UseTelemetry", Custom: "OnTelemetry"),

        new("Contract", Local: "Contract", External: "UseContract", Custom: "OnContract"),

        // The fleet: other agents, reachable as a registry the way every resource is — a local
        // folder of agent documents, a provider's registry, or a delegate. A registered agent
        // materializes as a tool, so the perimeter that governs acts governs handoffs.
        new("Agents", Local: "Agents", External: "UseAgents", Custom: "OnAgents"),

        // Found missing in the 2026-08-23 sweep: a full capability nature — its own broker
        // seam, four decorators, a rule-backed default — absent from this matrix entirely, with
        // one mode on the client (.Redact(), hardcoded to the default rules), no waiver, and no
        // way for a host to supply its own rules or broker. The matrix could not see it because
        // nothing ties the matrix to the surface; the null-Local waiver test below closes the
        // half of that hole that is closeable from inside the matrix.
        new("Redaction", Local: "Redact", External: "UseRedaction", Custom: "OnRedaction")
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
            if (capability.Local is string local)
            {
                modes.Add(capability.Name, "Local", local);
            }

            modes.Add(capability.Name, "External", capability.External);
            modes.Add(capability.Name, "Custom", capability.Custom);
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

    // The "pending" waivers are retired: Skills, Memory and Knowledge all have delegate forms
    // now. What remains is not debt but a documented impossibility — SPEC.md §4.8 permits a mode
    // to be omitted where it is genuinely meaningless, provided the reason is stated, and a Brain
    // that runs in-process cannot exist in a core that has no inference dependency.
    //
    // The distinction is the whole point: a waiver saying "not yet" is debt, and a waiver saying
    // "not possible, here is why" is a design decision. Only the second kind may survive a
    // release, and every omitted mode still has to say which it is.
    [Fact]
    public void ShouldOnlyWaiveCapabilityModesThatCannotExist()
    {
        // given
        string[] expectedWaivedCapabilities = ["Brain", "NativeBrain"];

        // when
        Capability[] actualWaived =
            [.. capabilities.Where(capability => capability.Waiver is not null)];

        // then
        actualWaived.Select(capability => capability.Name)
            .Should().BeEquivalentTo(
                expectedWaivedCapabilities,
                because: "the only permitted omission is one that cannot exist, and every "
                    + "other capability must answer Local, External and Custom");

        actualWaived.Should().OnlyContain(capability =>
            capability.Waiver!.Contains("N/A")
                && capability.Waiver!.Contains(':')
                && capability.Waiver!.Split(':').Last().Trim().Length > 10,
            because: "a waiver reading 'pending' is debt, and a bare 'N/A' is a verdict "
                + "without a reason — the reason is what makes waiving a reviewed decision");
    }

    // The sharpest hole the sweep found in this file: a capability declared with Local: null and
    // Waiver: null was silently unasserted — the Local theory row was skipped and the waiver-set
    // equivalence still passed, so the omission was exactly the one nobody sees. Derived, not
    // enumerated: any future row that skips a mode without saying why fails here.
    [Fact]
    public void ShouldWaiveEveryModeItSkips()
    {
        // given, when
        Capability[] silentOmissions =
            [.. capabilities.Where(capability =>
                capability.Local is null && capability.Waiver is null)];

        // then
        silentOmissions.Should().BeEmpty(
            because: "a mode that genuinely does not apply is waived in code with its reason — "
                + "an unwaived omission is the erosion this matrix exists to prevent");
    }
}
