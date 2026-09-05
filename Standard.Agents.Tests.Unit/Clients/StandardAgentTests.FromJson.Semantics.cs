// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Standard.Agents.Models.Clients.Agents.Exceptions;
using Xunit;

namespace Standard.Agents.Tests.Unit.Clients;

// Found in the 2026-09-04 principal review (F-24): a top-level typo was refused, but a document
// could still look valid and be ineffective. A nested typo was ignored, a wrong-typed value
// surfaced as a raw runtime exception or fell back to a default, a zero limit composed as if it
// were set, a section given the wrong shape collapsed into defaults, and a control that listed
// nothing read as on. Every one of those is now refused, by name, before an agent composes.
public partial class StandardAgentFromJsonTests
{
    private static void ShouldRefuse(string json, string expectedMessage)
    {
        // given
        var expectedInvalidAgentConfigurationException =
            new InvalidAgentConfigurationException(message: expectedMessage);

        // when
        Action composeAction = () => StandardAgent.FromJson(json);

        InvalidAgentConfigurationException actualInvalidAgentConfigurationException =
            Assert.Throws<InvalidAgentConfigurationException>(composeAction);

        // then
        actualInvalidAgentConfigurationException.Should()
            .BeEquivalentTo(expectedInvalidAgentConfigurationException);
    }

    [Theory]
    [InlineData(
        """{ "brain": { "apiUrl": "http://b.test/v1/", "model": "m", "temprature": 0.1 } }""",
        "'brain' does not accept 'temprature'. It accepts: apiUrl, apiKey, model, temperature, "
            + "maxTokens, timeoutSeconds.")]
    [InlineData(
        """{ "knowledge": { "path": "Knowledge", "maxResult": 5 } }""",
        "'knowledge' does not accept 'maxResult'. It accepts: path, pattern, maxResults, minScore.")]
    [InlineData(
        """{ "sessions": { "path": "Sessions", "maxHistory": 5 } }""",
        "'sessions' does not accept 'maxHistory'. It accepts: path, maxHistoryTurns.")]
    [InlineData(
        """{ "budget": { "maxTokens": 100, "maxCost": 1 } }""",
        "'budget' does not accept 'maxCost'. It accepts: maxTokens, maxCostUsd, "
            + "maxWallClockSeconds, costPerThousandTokens.")]
    [InlineData(
        """{ "mcp": { "endpointUrl": "http://mcp.test/", "token": "t" } }""",
        "'mcp' does not accept 'token'. It accepts: endpointUrl, relativeUrl, timeoutSeconds, "
            + "bearerToken, apiKey, apiKeyHeader.")]
    [InlineData(
        """{ "redact": { "rules": [ { "label": "SSN", "regex": "\\d+" } ] } }""",
        "'redact.rules' does not accept 'regex'. It accepts: label, pattern.")]
    public void ShouldThrowInvalidAgentConfigurationExceptionOnFromJsonIfANestedKeyIsUnknown(
        string json,
        string expectedMessage) =>
        ShouldRefuse(json, expectedMessage);

    [Theory]
    [InlineData(
        """{ "brain": { "apiUrl": "http://b.test/v1/", "model": "m", "maxTokens": "lots" } }""",
        "'brain.maxTokens' must be a number.")]
    [InlineData(
        """{ "knowledge": { "path": "Knowledge", "minScore": "high" } }""",
        "'knowledge.minScore' must be a number.")]
    [InlineData(
        """{ "budget": { "maxCostUsd": "cheap" } }""",
        "'budget.maxCostUsd' must be a number.")]
    [InlineData(
        """{ "usage": { "charactersPerToken": true } }""",
        "'usage.charactersPerToken' must be a number.")]
    public void ShouldThrowInvalidAgentConfigurationExceptionOnFromJsonIfANumberIsNotANumber(
        string json,
        string expectedMessage) =>
        ShouldRefuse(json, expectedMessage);

    [Theory]
    [InlineData("""{ "maxTurns": 0 }""", "'maxTurns' must be a positive number.")]
    [InlineData("""{ "resilience": -1 }""", "'resilience' must be zero or a positive number.")]
    [InlineData(
        """{ "brain": { "apiUrl": "http://b.test/v1/", "model": "m", "timeoutSeconds": 0 } }""",
        "'brain.timeoutSeconds' must be a positive number.")]
    [InlineData(
        """{ "brain": { "apiUrl": "http://b.test/v1/", "model": "m", "maxTokens": -5 } }""",
        "'brain.maxTokens' must be a positive number.")]
    [InlineData(
        """{ "knowledge": { "path": "Knowledge", "maxResults": 0 } }""",
        "'knowledge.maxResults' must be a positive number.")]
    [InlineData(
        """{ "sessions": { "path": "Sessions", "maxHistoryTurns": 0 } }""",
        "'sessions.maxHistoryTurns' must be a positive number.")]
    [InlineData(
        """{ "usage": { "charactersPerToken": 0 } }""",
        "'usage.charactersPerToken' must be a positive number.")]
    [InlineData(
        """{ "budget": { "maxTokens": 0 } }""",
        "'budget.maxTokens' must be a positive number.")]
    [InlineData(
        """{ "budget": { "maxWallClockSeconds": -1 } }""",
        "'budget.maxWallClockSeconds' must be a positive number.")]
    [InlineData(
        """{ "mcp": { "endpointUrl": "http://mcp.test/", "timeoutSeconds": 0 } }""",
        "'mcp.timeoutSeconds' must be a positive number.")]
    public void ShouldThrowInvalidAgentConfigurationExceptionOnFromJsonIfALimitIsNotPositive(
        string json,
        string expectedMessage) =>
        ShouldRefuse(json, expectedMessage);

    [Theory]
    [InlineData("""{ "usage": "fast" }""", "'usage' must be an object.")]
    [InlineData("""{ "budget": 5 }""", "'budget' must be an object.")]
    [InlineData("""{ "brain": "http://b.test/v1/" }""", "'brain' must be an object.")]
    [InlineData("""{ "risk": [ "wire" ] }""", "'risk' must be an object.")]
    public void ShouldThrowInvalidAgentConfigurationExceptionOnFromJsonIfASectionHasTheWrongShape(
        string json,
        string expectedMessage) =>
        ShouldRefuse(json, expectedMessage);

    [Theory]
    [InlineData("""{ "ruleGate": [] }""", "ruleGate")]
    [InlineData("""{ "ruleJudge": [] }""", "ruleJudge")]
    [InlineData("""{ "requireApproval": [] }""", "requireApproval")]
    [InlineData("""{ "redact": { "rules": [] } }""", "redact.rules")]
    [InlineData("""{ "risk": {} }""", "risk")]
    [InlineData("""{ "skills": [] }""", "skills")]
    [InlineData("""{ "mcp": [] }""", "mcp")]
    [InlineData("""{ "agents": [] }""", "agents")]
    public void ShouldThrowInvalidAgentConfigurationExceptionOnFromJsonIfAControlListsNothing(
        string json,
        string key) =>
        ShouldRefuse(
            json,
            $"'{key}' lists nothing. A control that lists nothing is not a control: remove the "
                + "key, or list what it should hold.");
}
