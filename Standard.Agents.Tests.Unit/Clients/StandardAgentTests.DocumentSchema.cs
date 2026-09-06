// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Text.Json.Nodes;
using FluentAssertions;
using Standard.Agents.Models.Clients.Agents.Exceptions;
using Xunit;

namespace Standard.Agents.Tests.Unit.Clients;

// Found in the 2026-09-04 principal review (F-24): the agent document had no schema an editor
// could read. A hand-written one would drift from the switch that is the truth, so the schema is
// EMITTED from the same table the document is validated against: every key it names composes,
// every key it does not name is refused, and every nested property it lists is one the section
// accepts, because there is only one list.
public partial class StandardAgentFromJsonTests
{
    private static JsonObject Schema() =>
        JsonNode.Parse(StandardAgent.DocumentSchemaJson())!.AsObject();

    [Fact]
    public void ShouldEmitAClosedSchemaForTheDocument()
    {
        // when
        JsonObject schema = Schema();

        // then — a document is an object with only the keys the builder knows
        schema["$schema"]!.GetValue<string>().Should().Contain("json-schema.org");
        schema["type"]!.GetValue<string>().Should().Be("object");
        schema["additionalProperties"]!.GetValue<bool>().Should().BeFalse();
        schema["properties"]!.AsObject().Count.Should().BeGreaterThan(30);
    }

    [Fact]
    public void ShouldComposeEveryExampleTheSchemaCarries()
    {
        // given — each key's example, as the schema shows it to an editor
        JsonObject properties = Schema()["properties"]!.AsObject();

        foreach ((string key, JsonNode? description) in properties)
        {
            JsonNode example = description!["examples"]![0]!;
            string document = new JsonObject { [key] = example.DeepClone() }.ToJsonString();

            // when
            Action composing = () => StandardAgent.FromJson(document);

            // then — an example the builder refuses is a lie in the editor
            composing.Should().NotThrow(because: $"the schema's example for '{key}' must compose");
        }
    }

    [Fact]
    public void ShouldRefuseAKeyTheSchemaDoesNotName()
    {
        // given
        JsonObject properties = Schema()["properties"]!.AsObject();
        properties.ContainsKey("buget").Should().BeFalse();

        // when
        Action composing = () => StandardAgent.FromJson("""{ "buget": { "maxTokens": 50000 } }""");

        // then
        composing.Should().Throw<InvalidAgentConfigurationException>().WithMessage("*buget*");
    }

    [Fact]
    public void ShouldCloseEveryObjectSectionInTheSchema()
    {
        // given
        JsonObject properties = Schema()["properties"]!.AsObject();

        // when . then — every object shape names its properties and admits nothing else
        foreach ((string key, JsonNode? description) in properties)
        {
            foreach (JsonNode? shape in description!["anyOf"]!.AsArray())
            {
                if (shape!["type"]?.GetValue<string>() == "object")
                {
                    shape["additionalProperties"]!.GetValue<bool>().Should().BeFalse(
                        because: $"'{key}' is validated against a closed property list");
                }
            }
        }
    }
}
