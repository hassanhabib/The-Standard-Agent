// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Brokers.Redactions;
using Standard.Agents.Models.Foundations.Brains;
using Standard.Agents.Services.Foundations.Brains;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Brains;

public partial class BrainServiceTests
{
    [Fact]
    public async Task ShouldRedactPiiBeforeBrainAndRehydrateStreamedReplyOnGenerateStreamAsync()
    {
        // given
        var emailRule = new RedactionRule
        {
            Label = "EMAIL",
            Pattern = @"[\w.+-]+@[\w-]+\.[\w.-]+"
        };

        var redactingBrainService = new BrainService(
            generatorBroker: this.generatorBrokerMock.Object,
            loggingBroker: this.loggingBrokerMock.Object,
            redactionBroker: new RuleRedactionBroker([emailRule]));

        string userPrompt = "email jane@acme.com the report";

        // the brain echoes the token, split across two chunks
        this.generatorBrokerMock.Setup(broker =>
            broker.GenerateStreamAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Returns(ToAsyncStream("sent to {{EMA", "IL_0}} now"));

        // when
        List<string> tokens = await DrainAsync(
            redactingBrainService.GenerateStreamAsync("system", userPrompt));

        string joined = string.Concat(tokens);

        // then — the brain never saw the address
        this.generatorBrokerMock.Verify(broker =>
            broker.GenerateStreamAsync(
                It.IsAny<string>(),
                It.Is<string>(prompt =>
                    prompt.Contains("{{EMAIL_0}}")
                        && prompt.Contains("jane@acme.com") == false),
                It.IsAny<CancellationToken>()),
                Times.Once);

        // and the streamed answer is rehydrated, even across the chunk split
        joined.Should().Be("sent to jane@acme.com now");
        joined.Should().NotContain("{{EMAIL_0}}");
    }
}
