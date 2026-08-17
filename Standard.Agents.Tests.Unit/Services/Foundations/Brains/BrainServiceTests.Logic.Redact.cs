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
    public async Task ShouldRedactPiiBeforeBrainAndRehydrateReplyOnGenerateAsync()
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

        string systemPrompt = CreateRandomString();
        string userPrompt = "please email jane@acme.com the report";

        this.generatorBrokerMock.Setup(broker =>
            broker.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("sure, sending it to {{EMAIL_0}} now");

        // when
        string actualReply =
            await redactingBrainService.GenerateAsync(systemPrompt, userPrompt);

        // then — the brain never saw the real email
        this.generatorBrokerMock.Verify(broker =>
            broker.GenerateAsync(
                It.IsAny<string>(),
                It.Is<string>(prompt =>
                    prompt.Contains("{{EMAIL_0}}")
                        && prompt.Contains("jane@acme.com") == false)),
                Times.Once);

        // and the caller got the real email back
        actualReply.Should().Be("sure, sending it to jane@acme.com now");
    }
}
