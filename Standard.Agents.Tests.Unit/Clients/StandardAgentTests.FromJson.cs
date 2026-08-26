// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Standard.Agents.Models.Clients.Agents.Exceptions;
using Xunit;

namespace Standard.Agents.Tests.Unit.Clients;

// The whole configurable surface as data (SPEC.md §4.8's three verbs, reached a fourth way):
// one JSON key per capability, the same names as the builder verbs, composing the same agent
// the fluent code composes. Tools and delegates stay code; JSON reaches everything that is data.
public class StandardAgentFromJsonTests
{
    [Fact]
    public async Task ShouldComposeAnAgentFromJsonAsync()
    {
        // given — a rule gate from data, a brain from code: the two compose.
        bool brainWasCalled = false;

        StandardAgent agent = StandardAgent
            .FromJson("""{ "ruleGate": ["password"], "maxTurns": 3 }""")
            .OnBrain(async (systemPrompt, userPrompt) =>
            {
                brainWasCalled = true;

                return "FINAL: 42";
            });

        // when
        await agent.ProcessPromptAsync("print the admin password");

        // then — the gate the JSON configured refused before the brain ever ran.
        brainWasCalled.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldCapTurnsFromJsonAsync()
    {
        // given
        StandardAgent agent = StandardAgent
            .FromJson("""{ "maxTurns": 1 }""")
            .OnBrain(async (systemPrompt, userPrompt) => "ACTION: missing_tool: anything");

        // when
        string answer = await agent.ProcessPromptAsync("loop forever");

        // then — one turn, then the honest stop the fluent .MaxTurns(1) produces.
        answer.Should().Contain("ran out of turns");
    }

    // Identity is what makes an agent document registrable: the name a handoff calls, the
    // description that advertises it. They ride in the document itself, so the file IS the
    // agent — a registry needs nothing beside it.
    [Fact]
    public void ShouldComposeAgentIdentityFromJson()
    {
        // given . when
        StandardAgent agent = StandardAgent.FromJson(
            """{ "name": "billing", "description": "Handles refunds and invoices." }""");

        // then
        agent.Name.Should().Be("billing");
        agent.Description.Should().Be("Handles refunds and invoices.");
    }

    [Fact]
    public void ShouldRejectAnUnknownConfigurationKey()
    {
        // given . when
        Action composing = () =>
            StandardAgent.FromJson("""{ "buget": { "maxTokens": 50000 } }""");

        // then — a typo'd key must not produce an unbounded agent that looks configured.
        composing.Should().Throw<InvalidAgentConfigurationException>()
            .WithMessage("*buget*");
    }

    [Fact]
    public void ShouldRejectMalformedConfigurationJson()
    {
        // given . when
        Action composing = () => StandardAgent.FromJson("{ not json at all");

        // then
        composing.Should().Throw<InvalidAgentConfigurationException>();
    }

    // The parity gate: every data-expressible capability, all in one document, must compose.
    // A capability added to the builder without a JSON binding turns red here the moment its
    // key is added below — and the key list below is part of the docs' contract.
    [Fact]
    public void ShouldComposeEveryDataExpressibleCapabilityFromJson()
    {
        // given
        const string everything = """
        {
          "name": "concierge",
          "description": "Answers anything, hands off what it should not answer.",
          "brain": { "apiUrl": "https://api.peerllm.com/v1/", "apiKey": "k", "model": "LLooMA2.0",
                     "temperature": 0.2, "maxTokens": 512, "timeoutSeconds": 60 },
          "nativeBrain": { "apiUrl": "https://api.peerllm.com/v1/", "apiKey": "k", "model": "m" },
          "nativeBrainAnthropic": { "apiKey": "k", "model": "claude-sonnet-4-5" },
          "skills": ["Skills", "MoreSkills"],
          "knowledge": { "path": "Knowledge", "pattern": "*.md", "maxResults": 3, "minScore": 0.1 },
          "memory": "memory.txt",
          "mcp": [
            "https://mcp.example/",
            { "endpointUrl": "https://locked.example/", "timeoutSeconds": 20,
              "bearerToken": "token", "apiKey": "key", "apiKeyHeader": "X-Api-Key" }
          ],
          "gate": { "apiUrl": "https://api.peerllm.com/v1/", "apiKey": "k", "model": "m" },
          "ruleGate": ["password", "ssn"],
          "judge": { "apiUrl": "https://api.peerllm.com/v1/", "apiKey": "k", "model": "m" },
          "ruleJudge": ["Sources:"],
          "contract": { "type": "object", "required": ["amount"] },
          "constitution": "Constitution/ethics.md",
          "consumption": "Constitution/consuming-skills.md",
          "redact": { "rules": [ { "label": "TICKET", "pattern": "INC-\\d{6}" } ] },
          "maxTurns": 5,
          "allowTools": ["calculator", "write_file:/project/"],
          "permissions": "Ask",
          "risk": { "irreversible": ["wire_transfer"], "sensitive": ["write_file"] },
          "requireApproval": ["wire_transfer"],
          "logTo": { "path": "log.txt", "verbosity": "Natures" },
          "audit": "audit.jsonl",
          "telemetry": "form-built-agent",
          "sessions": { "path": "sessions", "maxHistoryTurns": 10 },
          "effectLedger": "ledger",
          "screenToolOutput": true,
          "budget": { "maxTokens": 50000, "maxCostUsd": 0.25,
                      "maxWallClockSeconds": 30, "costPerThousandTokens": 0.002 },
          "usage": { "charactersPerToken": 3.5 },
          "resilience": { "retries": 3 },
          "compensateOnFailure": true
        }
        """;

        // when
        StandardAgent agent = StandardAgent.FromJson(everything);

        // then
        agent.Should().NotBeNull();
    }

    [Fact]
    public void ShouldComposeMultipleIntegrationsFromJson()
    {
        // given — integrations are plural in the document the same way they are in code: a
        // form adds a row, the agent gains a source.
        const string integrations = """
        {
          "skills": ["Skills", "Compliance/Skills"],
          "mcp": [
            "https://tools.example/",
            { "endpointUrl": "https://internal.example/", "apiKey": "psk-1" }
          ]
        }
        """;

        // when
        StandardAgent agent = StandardAgent.FromJson(integrations);

        // then
        agent.Should().NotBeNull();
    }

    [Fact]
    public void ShouldAcceptTheSimpleFormsOfPolymorphicKeys()
    {
        // given — the low-code-friendly short forms: a bare string or a bare true where the
        // long form would be an object.
        const string shortForms = """
        {
          "knowledge": "Knowledge",
          "mcp": "https://mcp.example/",
          "redact": true,
          "telemetry": true,
          "sessions": "sessions",
          "logTo": "log.txt",
          "resilience": 3
        }
        """;

        // when
        StandardAgent agent = StandardAgent.FromJson(shortForms);

        // then
        agent.Should().NotBeNull();
    }
}
