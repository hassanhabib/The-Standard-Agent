// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Text.Json.Nodes;
using FluentAssertions;
using Standard.Agents.Models.Brokers.Generators;
using Xunit;

namespace Standard.Agents.Tests.Unit.Wire;

// The core-owned-keys rule (docs/per-request-inference.md §4.4). ProviderOptionsJson is
// inference-shaping only — and that bound is only real if the merge enforces it. The wire
// carries `tools` and `messages`; a naive merge of a passthrough containing either would widen
// the perimeter at the wire level, which is precisely what §3 promises cannot happen.
public class ProviderOptionsTests
{
    [Fact]
    public void ShouldStripEveryCoreOwnedKeyAndReportEachOne()
    {
        // given — a passthrough trying to touch what the core writes
        string providerOptionsJson = """
            {
                "temperature": 2.0,
                "tools": [{"type": "function"}],
                "messages": [],
                "response_format": {"type": "json_object"},
                "max_tokens": 999999,
                "chat_template_kwargs": {"enable_thinking": false}
            }
            """;

        // when
        SanitizedProviderOptions sanitized = ProviderOptions.Sanitize(providerOptionsJson);

        // then — the modeled fields win, and the collisions are named so a trace can say so
        sanitized.Malformed.Should().BeFalse();

        sanitized.StrippedKeys.Should().BeEquivalentTo(
            "temperature", "tools", "messages", "response_format", "max_tokens");

        JsonObject survived = JsonNode.Parse(sanitized.Json!)!.AsObject();
        survived.Should().ContainSingle();
        survived.ContainsKey("chat_template_kwargs").Should().BeTrue();
    }

    [Fact]
    public void ShouldPassEngineKeysThroughWhole()
    {
        // given — what the core cannot model and should not try to
        string providerOptionsJson = """
            {
                "grammar": "root ::= object",
                "thinking": {"type": "enabled", "budget_tokens": 1024}
            }
            """;

        // when
        SanitizedProviderOptions sanitized = ProviderOptions.Sanitize(providerOptionsJson);

        // then
        sanitized.StrippedKeys.Should().BeEmpty();

        JsonObject survived = JsonNode.Parse(sanitized.Json!)!.AsObject();
        survived["grammar"]!.GetValue<string>().Should().Be("root ::= object");
        survived["thinking"]!["budget_tokens"]!.GetValue<int>().Should().Be(1024);
    }

    [Fact]
    public void ShouldTreatAMalformedBagAsNothingAndSayItWasMalformed()
    {
        // when
        SanitizedProviderOptions sanitized = ProviderOptions.Sanitize("not json at all");

        // then — nothing reaches the wire, and the boundary can say why
        sanitized.Json.Should().BeNull();
        sanitized.Malformed.Should().BeTrue();
    }

    [Fact]
    public void ShouldTreatAnAbsentBagAsNothingQuietly()
    {
        // when
        SanitizedProviderOptions sanitized = ProviderOptions.Sanitize(null);

        // then
        sanitized.Json.Should().BeNull();
        sanitized.StrippedKeys.Should().BeEmpty();
        sanitized.Malformed.Should().BeFalse();
    }

    [Fact]
    public void ShouldMergeSanitizedKeysOntoTheRequestWhole()
    {
        // given — the request the broker built, and a sanitized bag
        var request = new JsonObject
        {
            ["model"] = "test",
            ["temperature"] = 0.7
        };

        string sanitizedJson = """{"chat_template_kwargs": {"enable_thinking": false}}""";

        // when
        ProviderOptions.MergeInto(request, sanitizedJson);

        // then — the engine key landed and the core's keys stand untouched
        request["chat_template_kwargs"]!["enable_thinking"]!.GetValue<bool>().Should().BeFalse();
        request["temperature"]!.GetValue<double>().Should().Be(0.7);
        request["model"]!.GetValue<string>().Should().Be("test");
    }

    // Belt and braces: even if a raw, unsanitized bag reached the merge, a core-owned key still
    // cannot land. Two independent enforcement points, because this is the perimeter.
    [Fact]
    public void ShouldRefuseToMergeACoreOwnedKeyEvenWhenHandedOneRaw()
    {
        // given
        var request = new JsonObject { ["temperature"] = 0.7 };
        string rawJson = """{"temperature": 2.0, "grammar": "root ::= object"}""";

        // when
        ProviderOptions.MergeInto(request, rawJson);

        // then
        request["temperature"]!.GetValue<double>().Should().Be(0.7);
        request["grammar"]!.GetValue<string>().Should().Be("root ::= object");
    }
}
