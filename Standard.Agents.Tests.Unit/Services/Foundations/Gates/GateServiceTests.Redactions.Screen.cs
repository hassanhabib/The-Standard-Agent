// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Brokers.Classifiers;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Brokers.Redactions;
using Standard.Agents.Models.Foundations.Brains;
using Standard.Agents.Services.Foundations.Gates;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Gates;

// SPEC.md §4.6: redaction covers every model the agent drives, not only the Brain. The Gate
// screens the raw task, so it sees exactly the values redaction exists to hide — and a guardian
// may run on a different host than the Brain, which makes an unredacted Gate a wider exposure
// than an unredacted Brain, not a narrower one.
public partial class GateServiceTests
{
    private static IGateService RedactingGate(
        Mock<IClassifierBroker> classifierBrokerMock,
        Mock<ILoggingBroker> loggingBrokerMock) =>
        new GateService(
            classifierBroker: classifierBrokerMock.Object,
            loggingBroker: loggingBrokerMock.Object,
            redactionBroker: new RuleRedactionBroker(RedactionRules.Default));

    [Fact]
    public async Task ShouldRedactSensitiveValuesBeforeScreeningAsync()
    {
        // given
        string sensitiveEmail = "ada@example.com";
        string inputWithPii = $"email the invoice to {sensitiveEmail}";
        string capturedInput = string.Empty;

        var classifierBrokerMock = new Mock<IClassifierBroker>();

        classifierBrokerMock.Setup(broker => broker.ClassifyAsync(It.IsAny<string>()))
            .Callback<string>(input => capturedInput = input)
            .ReturnsAsync("accept");

        IGateService gateService =
            RedactingGate(classifierBrokerMock, new Mock<ILoggingBroker>());

        // when
        await gateService.ScreenAsync(inputWithPii);

        // then
        capturedInput.Should().NotContain(sensitiveEmail);
        capturedInput.Should().Contain("EMAIL");
    }

    [Fact]
    public async Task ShouldNotRedactWhenNoRedactionIsConfiguredOnScreenAsync()
    {
        // given
        string sensitiveEmail = "ada@example.com";
        string inputWithPii = $"email the invoice to {sensitiveEmail}";
        string capturedInput = string.Empty;

        this.classifierBrokerMock.Setup(broker => broker.ClassifyAsync(It.IsAny<string>()))
            .Callback<string>(input => capturedInput = input)
            .ReturnsAsync("accept");

        // when
        await this.gateService.ScreenAsync(inputWithPii);

        // then — redaction is opt-in; absent it the agent behaves as if §4.6 did not exist
        capturedInput.Should().Be(inputWithPii);
    }
}
