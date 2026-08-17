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

    public static TheoryData<string> ServicesAboveTheFoundationTier()
    {
        var services = new TheoryData<string>();

        foreach (Type service in ServicesUnder("Orchestrations").Concat(ServicesUnder("Coordinations")))
        {
            services.Add(service.Name);
        }

        return services;
    }

    // A tier that reaches a broker skips the foundation that would have given it validation,
    // exception mapping and attribution. That is how a full disk came to be blamed on Direction.
    [Theory]
    [MemberData(nameof(ServicesAboveTheFoundationTier))]
    public void ShouldTakeNoBrokerAboveTheFoundationTierBeyondTheUtilities(string serviceName)
    {
        // given
        Type service = ServicesUnder("Orchestrations")
            .Concat(ServicesUnder("Coordinations"))
            .Single(type => type.Name == serviceName);

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
}
