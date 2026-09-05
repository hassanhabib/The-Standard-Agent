// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Reflection;
using FluentAssertions;
using Xunit;

namespace Standard.Agents.Tests.Unit.Architecture;

public partial class TierDisciplineTests
{
    private static IEnumerable<Type> Brokers() =>
        agentAssembly.GetTypes()
            .Where(type => type.IsClass && type.IsAbstract is false)
            .Where(type => type.Namespace?.Contains(".Brokers.", StringComparison.Ordinal) is true)
            .Where(type => type.Name.EndsWith("Broker", StringComparison.Ordinal));

    private static IEnumerable<Type> BrokerInterfacesTakenBy(ConstructorInfo constructor) =>
        constructor.GetParameters()
            .SelectMany(parameter => TypesReferencedBy(
                Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType))
            .Where(type => type.IsInterface && type.Name.EndsWith("Broker", StringComparison.Ordinal))
            .Distinct();

    // Found in the 2026-09-04 principal review (F-16): the logging broker held the time broker
    // and the audit broker, orchestrated both, and the alignment document said no deviation
    // remained. A broker is one resource, wrapped; brokers cannot call other brokers. The one
    // shape that may hold a broker is a DECORATOR: a broker that implements the very interface
    // it takes, so it wraps another broker's call and adds one concern (retry, redaction, audit)
    // without owning a resource of its own. This reads the constructors, so the dependency FLOW
    // is what is checked, not the folder a type sits in.
    [Fact]
    public void ShouldHoldNoOtherBrokerUnlessDecoratingOne()
    {
        // given
        var offenders = new List<string>();

        // when
        foreach (Type broker in Brokers())
        {
            Type[] implemented = broker.GetInterfaces();

            foreach (ConstructorInfo constructor in broker.GetConstructors())
            {
                Type[] taken = [.. BrokerInterfacesTakenBy(constructor)];

                bool decorates = taken.Any(implemented.Contains);

                if (taken.Length > 0 && decorates is false)
                {
                    offenders.Add(
                        $"{broker.Name} takes [{string.Join(", ", taken.Select(type => type.Name))}]");
                }
            }
        }

        // then
        offenders.Should().BeEmpty(
            because: "a broker is one resource, wrapped, and brokers cannot call other brokers; "
                + "the only broker that may hold another is a decorator, which implements the "
                + "interface it takes and adds one concern to the call it wraps");
    }
}
