// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Reflection;
using FluentAssertions;
using Xunit;

namespace Standard.Agents.Tests.Unit.Architecture;

// The Standard's tier rules, as a test rather than as a convention.
//
// The enterprise program put five brokers where brokers are not allowed — an orchestration
// holding three, coordination holding two, and a broker holding two more — and nothing failed.
// Every one of those was invisible until someone read the constructors, which is exactly the kind
// of erosion a build should catch (docs/architecture-alignment.md).
//
// So: a foundation wraps ONE nature broker. Anything above the foundation tier holds no broker at
// all. Utility brokers are the documented exception, and the list of them is short and named here
// so that adding a sixth is a reviewed decision rather than an omission nobody sees.
public class TierDisciplineTests
{
    // Observability, plus the clock. None backs a nature; none can change what the agent decides.
    // Resilience is deliberately NOT here: it changes control flow, so it does not get logging's
    // exemption — it is applied by decorating a broker at composition.
    private static readonly string[] utilityBrokers =
        ["ILoggingBroker", "ITimeBroker", "IAuditBroker"];

    private static readonly Assembly agentAssembly = typeof(StandardAgent).Assembly;

    private static IEnumerable<Type> ServicesUnder(string tierFolder) =>
        agentAssembly.GetTypes()
            .Where(type => type.IsClass && type.IsAbstract is false)
            .Where(type => type.Namespace is not null)
            .Where(type => type.Namespace!.Contains($".Services.{tierFolder}", StringComparison.Ordinal))
            .Where(type => type.Name.EndsWith("Service", StringComparison.Ordinal));

    // Per CONSTRUCTOR, not per type. MemoryService and KnowledgeService each offer two
    // constructors — one over their own broker, one over the file broker — and each takes exactly
    // one. Counting across overloads would read that as two and be wrong.
    private static IEnumerable<IEnumerable<string>> BrokerSetsTakenBy(Type type) =>
        type.GetConstructors().Select(constructor =>
            constructor.GetParameters()
                .Select(parameter => Nullable.GetUnderlyingType(parameter.ParameterType)
                    ?? parameter.ParameterType)
                .Where(parameterType => parameterType.IsInterface
                    && parameterType.Name.EndsWith("Broker", StringComparison.Ordinal))
                .Select(parameterType => Role(parameterType.Name))
                .Distinct());

    private static IEnumerable<string> BrokersTakenBy(Type type) =>
        BrokerSetsTakenBy(type).SelectMany(set => set).Distinct();

    // One broker ROLE, which may be versioned: IGeneratorBrokerV1 is the same seam as
    // IGeneratorBroker, offered under a newer contract, and a foundation may hold both while
    // speaking to only one at a time.
    private static string Role(string brokerName) =>
        System.Text.RegularExpressions.Regex.Replace(brokerName, @"V\d+$", string.Empty);

    public static TheoryData<string> FoundationServices()
    {
        var services = new TheoryData<string>();

        foreach (Type service in ServicesUnder("Foundations"))
        {
            services.Add(service.Name);
        }

        return services;
    }

    [Theory]
    [MemberData(nameof(FoundationServices))]
    public void ShouldWrapExactlyOneNatureBrokerPerFoundation(string serviceName)
    {
        // given
        Type service = ServicesUnder("Foundations").Single(type => type.Name == serviceName);

        // when
        string[] worst =
            [.. BrokerSetsTakenBy(service)
                .Select(set => set.Where(broker => utilityBrokers.Contains(broker) is false))
                .OrderByDescending(set => set.Count())
                .FirstOrDefault() ?? []];

        // then — ReturnService is the dead end and wraps none; everything else wraps exactly one.
        worst.Length.Should().BeLessThanOrEqualTo(
            1,
            because: $"{serviceName} is a foundation, and a foundation wraps ONE broker — "
                + $"one of its constructors takes [{string.Join(", ", worst)}]. A second broker "
                + "here is a capability that belongs in its own foundation, or a concern that "
                + "belongs in a decorating broker at composition.");
    }

    // Every tier above foundations, named explicitly. A tier missing from this list is a tier
    // nobody is checking - which is how the loop briefly escaped the rule when it moved from
    // Coordinations to Managements.
    private static IEnumerable<Type> ServicesAboveFoundations() =>
        ServicesUnder("Orchestrations")
            .Concat(ServicesUnder("Coordinations"))
            .Concat(ServicesUnder("Managements"));

    public static TheoryData<string> ServicesAboveTheFoundationTier()
    {
        var services = new TheoryData<string>();

        foreach (Type service in ServicesAboveFoundations())
        {
            services.Add(service.Name);
        }

        return services;
    }

    // The 2-3 rule, which is the one that motivated the whole restructure and the one that was
    // never encoded. Every tier holds two or three of the tier directly below it: management over
    // coordinations, coordination over orchestrations, orchestration over foundations.
    //
    // Both bounds have a cost. More than three means the service is doing too much and wants
    // decomposition - that is what put six foundations under Direction. FEWER than two means the
    // opposite: a service with one dependency composes nothing, so it is a pass-through that adds
    // a layer and an exception hop for no work. Inference sat at one foundation for a whole
    // release with a comment in its interface arguing that it was "a region rather than a
    // pass-through", which is what arguing around a rule looks like from the inside.
    //
    // Utility brokers do not count. They are held by every tier and none of them is a dependency
    // in the sense the rule is about.
    [Theory]
    [MemberData(nameof(ServicesAboveTheFoundationTier))]
    public void ShouldHoldTwoOrThreeDependencies(string serviceName)
    {
        // given
        Type service = ServicesAboveFoundations().Single(type => type.Name == serviceName);

        // when
        int[] counts =
            [.. service.GetConstructors()
                .Select(constructor => constructor.GetParameters()
                    .Select(parameter => Nullable.GetUnderlyingType(parameter.ParameterType)
                        ?? parameter.ParameterType)
                    .Count(IsADependency))];

        // then
        counts.Should().OnlyContain(
            count => count >= 2 && count <= 3,
            because: $"{serviceName} holds [{string.Join(", ", counts)}] dependencies. Two or "
                + "three is the rule at every tier: more and the service is doing too much, "
                + "fewer and it composes nothing and is a layer for its own sake.");
    }

    // The other half of the same rule: two or three of the tier DIRECTLY below. Management over
    // coordinations, coordination over orchestrations, orchestration over foundations.
    //
    // Reaching further down is not a lesser version of the same mistake, it is a different one.
    // A count can be satisfied by anything; adjacency is what keeps a nature's foundations
    // reachable only through that nature. DirectionCoordinationService held Decision's Gate for
    // .ScreenToolOutput() and satisfied every other rule here while doing it — three
    // dependencies, no broker — which is why this one had to be written down separately.
    private static readonly Dictionary<string, string> tierBelow = new()
    {
        ["Managements"] = "Coordinations",
        ["Coordinations"] = "Orchestrations",
        ["Orchestrations"] = "Foundations"
    };

    [Theory]
    [MemberData(nameof(ServicesAboveTheFoundationTier))]
    public void ShouldDependOnlyOnTheTierDirectlyBelowIt(string serviceName)
    {
        // given
        Type service = ServicesAboveFoundations().Single(type => type.Name == serviceName);

        string tier = tierBelow.Keys.Single(name =>
            service.Namespace!.Contains($".Services.{name}", StringComparison.Ordinal));

        string permitted = $".Services.{tierBelow[tier]}";

        // when
        string[] reachedPastIt =
            [.. service.GetConstructors()
                .SelectMany(constructor => constructor.GetParameters())
                .Select(parameter => Nullable.GetUnderlyingType(parameter.ParameterType)
                    ?? parameter.ParameterType)
                .Where(IsADependency)
                .Where(dependency =>
                    dependency.Namespace!.Contains(permitted, StringComparison.Ordinal) is false)
                .Select(dependency => dependency.Name)
                .Distinct()];

        // then
        reachedPastIt.Should().BeEmpty(
            because: $"{serviceName} is a {tier[..^1]} service, so it composes "
                + $"{tierBelow[tier]} and nothing else — it takes "
                + $"[{string.Join(", ", reachedPastIt)}]. A service that reaches past the tier "
                + "below it borrows another nature's internals, and no test of counts or brokers "
                + "will ever see it.");
    }

    // A dependency for counting purposes is another SERVICE — the thing the tier below supplies.
    // Options, delegates and primitives are configuration rather than collaborators, and utility
    // brokers are exempt everywhere.
    private static bool IsADependency(Type parameterType) =>
        parameterType.IsInterface
            && parameterType.Name.EndsWith("Service", StringComparison.Ordinal)
            && parameterType.Namespace?.Contains(".Services.", StringComparison.Ordinal) is true;

    // A tier that reaches a broker skips the foundation that would have given it validation,
    // exception mapping and attribution. That is how a full disk came to be blamed on Direction.
    [Theory]
    [MemberData(nameof(ServicesAboveTheFoundationTier))]
    public void ShouldTakeNoBrokerAboveTheFoundationTierBeyondTheUtilities(string serviceName)
    {
        // given
        Type service = ServicesAboveFoundations().Single(type => type.Name == serviceName);

        // when
        string[] resourceBrokers =
            [.. BrokersTakenBy(service).Where(broker => utilityBrokers.Contains(broker) is false)];

        // then
        resourceBrokers.Should().BeEmpty(
            because: $"{serviceName} sits above the foundation tier and takes "
                + $"[{string.Join(", ", resourceBrokers)}]. A resource reached from here has no "
                + "validation, no exception mapping, and its failures are attributed to the "
                + "caller rather than to the resource.");
    }

    // The one at the bottom, and the last to be noticed. Invariant 3: brokers are thin.
    //
    // Stated precisely, because two shapes look like violations and are not:
    //
    //   A broker MUST NOT hold a NATURE broker other than the one it implements.
    //
    // A nature broker is defined here as one a foundation wraps — derived from the foundations
    // themselves rather than listed, so the rule cannot drift as foundations are added. That
    // leaves exactly two legitimate shapes: a decorator holding its own contract plus the
    // cross-cutting broker it applies (redaction, retry), and observability composing a clock and
    // a sink. Neither can change what the agent decides or does, which is precisely why neither
    // can become the hole that Policy, Approval and the effect ledger slipped through.
    [Fact]
    public void ShouldNotLetAnyBrokerDependOnANatureBroker()
    {
        // given
        // Derived sets can empty out silently — a renamed namespace and this rule passes
        // vacuously forever. A derivation that finds nothing is a broken derivation, not a
        // clean architecture.
        HashSet<string> natureBrokers =
            [.. ServicesUnder("Foundations")
                .SelectMany(BrokersTakenBy)
                .Where(broker => utilityBrokers.Contains(broker) is false)];

        Type[] brokers =
            [.. agentAssembly.GetTypes()
                .Where(type => type.IsClass && type.IsAbstract is false)
                .Where(type => type.Namespace?.Contains(".Brokers.", StringComparison.Ordinal) is true)
                .Where(type => type.Name.EndsWith("Broker", StringComparison.Ordinal))];

        // when
        var offenders = new List<string>();

        foreach (Type broker in brokers)
        {
            string[] foreign =
                [.. BrokersTakenBy(broker)
                    .Where(natureBrokers.Contains)
                    .Where(taken => broker.GetInterfaces().All(
                        contract => Role(contract.Name) != taken))];

            if (foreign.Length > 0)
            {
                offenders.Add($"{broker.Name} takes [{string.Join(", ", foreign)}]");
            }
        }

        // then
        offenders.Should().BeEmpty(
            because: "a broker is a thin liaison to ONE resource (Invariant 3). A broker that "
                + "reaches another nature's resource has business flow in it, and it is the "
                + "hardest kind to see because it sits underneath everything else.");
    }

    // "A tier missing from this list is a tier nobody is checking" was a comment; now it is a
    // rule. Every namespace segment under Standard.Agents.Services must be one of the four
    // checked tiers — when the loop moved to Managements, it left every rule in this file for
    // as long as nobody noticed, and a future tier gets no such window.
    [Fact]
    public void ShouldCheckEveryServiceTierThatExists()
    {
        // given
        string[] checkedTiers = ["Foundations", "Orchestrations", "Coordinations", "Managements"];

        // when
        string[] actualTiers =
            [.. agentAssembly.GetTypes()
                .Where(type => type.Namespace is not null)
                .Select(type => type.Namespace!)
                .Where(space => space.Contains(".Services.", StringComparison.Ordinal))
                .Select(space => space
                    .Split(".Services.", 2)[1]
                    .Split('.')[0])
                .Distinct()];

        // then
        actualTiers.Should().BeSubsetOf(
            checkedTiers,
            because: "a tier absent from the checked list is a tier nobody is checking, which "
                + "is how the loop's move to Managements went unwatched");
    }

    // The tool seam, found unscanned in the 2026-08-23 sweep. Tools are host-space: Direction
    // reaches them through ToolBroker, underneath the foundation tier — so a framework tool that
    // KEEPS a service or a broker re-enters the tier stack from below it. RememberTool held
    // IMemoryService, making the live chain InternalToolService → ToolBroker → RememberTool →
    // IMemoryService: a foundation transitively calling a foundation through a broker, and no
    // rule in this file could see it because nothing here scanned Standard.Agents.Tools at all.
    //
    // The rule is about what a tool HOLDS, which is why it scans fields rather than
    // constructors: a converting [Obsolete] constructor may still ACCEPT a service for
    // compatibility, but nothing of the tier stack may live in the tool. The client seam
    // (IAgent, in the root namespace) passes by construction — nesting a whole agent is
    // composition, not tier reach-through.
    [Fact]
    public void ShouldKeepFrameworkToolsOutOfTheTierStack()
    {
        // given
        IEnumerable<Type> frameworkTools = agentAssembly.GetTypes()
            .Where(type => type.IsClass && type.IsAbstract is false)
            .Where(type => type.Namespace is "Standard.Agents.Tools");

        // when
        List<string> offenders = [];

        foreach (Type tool in frameworkTools)
        {
            string[] held =
                [.. tool
                    .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .SelectMany(field => TypesReferencedBy(field.FieldType))
                    .Where(fieldType =>
                        fieldType.Namespace?.Contains(".Services.", StringComparison.Ordinal) is true
                            || fieldType.Namespace?.Contains(".Brokers.", StringComparison.Ordinal) is true)
                    .Select(fieldType => fieldType.Name)
                    .Distinct()];

            if (held.Length > 0)
            {
                offenders.Add($"{tool.Name} holds [{string.Join(", ", held)}]");
            }
        }

        // then
        offenders.Should().BeEmpty(
            because: "a tool is reached through a broker, underneath the foundations — a tool "
                + "holding a service or a broker re-enters the tier stack from below it, and "
                + "no adjacency rule can see the reach because it travels through data");
    }

    // Generic arguments count: IReadOnlyList<IMemoryService> holds the service as surely as the
    // bare field does.
    private static IEnumerable<Type> TypesReferencedBy(Type type)
    {
        yield return type;

        if (type.IsGenericType)
        {
            foreach (Type argument in type.GetGenericArguments())
            {
                foreach (Type referenced in TypesReferencedBy(argument))
                {
                    yield return referenced;
                }
            }
        }
    }
}
