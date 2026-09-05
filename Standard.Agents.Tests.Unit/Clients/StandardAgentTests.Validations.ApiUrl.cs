// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Standard.Agents.Models.Clients.Agents.Exceptions;
using Xunit;

namespace Standard.Agents.Tests.Unit.Clients;

public partial class StandardAgentTests
{
    // Found in the 2026-09-04 principal review (F-12): hosting.md taught an endpoint that
    // already named chat/completions, and the broker appends that route to whatever base it is
    // given, so the copied example reached v1/chat/chat/completions. The neighbouring mistake is
    // as silent: a base without its trailing slash resolves the route against the parent, so
    // 'https://host/v1' reaches 'https://host/chat/completions'. Both fail at the first prompt
    // with a 404 that blames the provider. The shape is refused at composition instead.
    public static TheoryData<string> InvalidApiUrls =>
        new()
        {
            "invalid-url",
            "https://api.peerllm.com/v1",
            "http://localhost:11434/v1/chat/completions",
            "https://api.peerllm.com/v1/chat/completions/"
        };

    private const string InvalidApiUrlMessage =
        "Invalid agent API URL. An endpoint is the base the route is appended to: an absolute "
            + "http(s) URL ending with '/', such as https://api.peerllm.com/v1/, that does not "
            + "name chat/completions itself.";

    [Theory]
    [MemberData(nameof(InvalidApiUrls))]
    public void ShouldThrowInvalidAgentApiUrlExceptionOnBrainIfApiUrlIsInvalid(string invalidApiUrl)
    {
        // given
        var expectedInvalidAgentApiUrlException =
            new InvalidAgentApiUrlException(message: InvalidApiUrlMessage);

        // when
        Action brainAction = () =>
            new StandardAgent().Brain(apiUrl: invalidApiUrl, apiKey: "key", model: "model");

        InvalidAgentApiUrlException actualInvalidAgentApiUrlException =
            Assert.Throws<InvalidAgentApiUrlException>(brainAction);

        // then
        actualInvalidAgentApiUrlException.Should()
            .BeEquivalentTo(expectedInvalidAgentApiUrlException);
    }

    [Theory]
    [MemberData(nameof(InvalidApiUrls))]
    public void ShouldThrowInvalidAgentApiUrlExceptionOnNativeBrainIfApiUrlIsInvalid(
        string invalidApiUrl)
    {
        // given
        var expectedInvalidAgentApiUrlException =
            new InvalidAgentApiUrlException(message: InvalidApiUrlMessage);

        // when
        Action nativeBrainAction = () =>
            new StandardAgent().NativeBrain(apiUrl: invalidApiUrl, apiKey: "key", model: "model");

        InvalidAgentApiUrlException actualInvalidAgentApiUrlException =
            Assert.Throws<InvalidAgentApiUrlException>(nativeBrainAction);

        // then
        actualInvalidAgentApiUrlException.Should()
            .BeEquivalentTo(expectedInvalidAgentApiUrlException);
    }

    [Theory]
    [MemberData(nameof(InvalidApiUrls))]
    public void ShouldThrowInvalidAgentApiUrlExceptionOnGateIfApiUrlIsInvalid(string invalidApiUrl)
    {
        // given
        var expectedInvalidAgentApiUrlException =
            new InvalidAgentApiUrlException(message: InvalidApiUrlMessage);

        // when
        Action gateAction = () =>
            new StandardAgent().Gate(apiUrl: invalidApiUrl, apiKey: "key", model: "model");

        InvalidAgentApiUrlException actualInvalidAgentApiUrlException =
            Assert.Throws<InvalidAgentApiUrlException>(gateAction);

        // then
        actualInvalidAgentApiUrlException.Should()
            .BeEquivalentTo(expectedInvalidAgentApiUrlException);
    }

    [Theory]
    [MemberData(nameof(InvalidApiUrls))]
    public void ShouldThrowInvalidAgentApiUrlExceptionOnJudgeIfApiUrlIsInvalid(string invalidApiUrl)
    {
        // given
        var expectedInvalidAgentApiUrlException =
            new InvalidAgentApiUrlException(message: InvalidApiUrlMessage);

        // when
        Action judgeAction = () =>
            new StandardAgent().Judge(apiUrl: invalidApiUrl, apiKey: "key", model: "model");

        InvalidAgentApiUrlException actualInvalidAgentApiUrlException =
            Assert.Throws<InvalidAgentApiUrlException>(judgeAction);

        // then
        actualInvalidAgentApiUrlException.Should()
            .BeEquivalentTo(expectedInvalidAgentApiUrlException);
    }
}
