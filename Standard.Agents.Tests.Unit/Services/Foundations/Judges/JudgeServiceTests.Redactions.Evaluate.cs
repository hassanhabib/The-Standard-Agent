// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Brokers.Redactions;
using Standard.Agents.Brokers.Verifiers;
using Standard.Agents.Models.Foundations.Brains;
using Standard.Agents.Services.Foundations.Judges;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Judges;

// SPEC.md §4.6: redaction covers every model. The Judge reads the task AND the drafted answer,
// so it sees more sensitive text than any other guardian — and it is the one most likely to be
// pointed at a cheap third-party endpoint, because scoring is the cheapest of the three calls.
public partial class JudgeServiceTests
{
    [Fact]
    public async Task ShouldRedactSensitiveValuesFromTaskAndCandidateOnEvaluateAsync()
    {
        // given
        string sensitiveEmail = "ada@example.com";
        string taskWithPii = $"draft a reply to {sensitiveEmail}";
        string candidateWithPii = $"I have emailed {sensitiveEmail} already";

        string capturedTask = string.Empty;
        string capturedCandidate = string.Empty;

        var verifierBrokerMock = new Mock<IVerifierBroker>();

        verifierBrokerMock.Setup(broker =>
            broker.VerifyAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string>((task, candidate) =>
                {
                    capturedTask = task;
                    capturedCandidate = candidate;
                })
                .ReturnsAsync("SCORE: 1.0\nREASON: ok");

        IJudgeService judgeService = new JudgeService(
            verifierBroker: verifierBrokerMock.Object,
            loggingBroker: new Mock<ILoggingBroker>().Object,
            redactionBroker: new RuleRedactionBroker(RedactionRules.Default));

        // when
        await judgeService.EvaluateAsync(taskWithPii, candidateWithPii);

        // then
        capturedTask.Should().NotContain(sensitiveEmail);
        capturedCandidate.Should().NotContain(sensitiveEmail);

        capturedTask.Should().Contain("EMAIL");
        capturedCandidate.Should().Contain("EMAIL");
    }

    [Fact]
    public async Task ShouldRehydrateTheJudgeReasonOnEvaluateAsync()
    {
        // given — a rejecting Judge quotes the answer back, and that reason becomes revision
        // feedback the Brain reads, so it must come back in the clear
        string sensitiveEmail = "ada@example.com";
        string candidateWithPii = $"I have emailed {sensitiveEmail} already";

        var verifierBrokerMock = new Mock<IVerifierBroker>();

        verifierBrokerMock.Setup(broker =>
            broker.VerifyAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((string task, string candidate) =>
                    $"SCORE: 0.1\nREASON: do not mention {ExtractToken(candidate)}");

        IJudgeService judgeService = new JudgeService(
            verifierBroker: verifierBrokerMock.Object,
            loggingBroker: new Mock<ILoggingBroker>().Object,
            redactionBroker: new RuleRedactionBroker(RedactionRules.Default));

        // when
        Models.Foundations.Judges.Judgement actualJudgement =
            await judgeService.EvaluateAsync("a task", candidateWithPii);

        // then
        actualJudgement.Reason.Should().Contain(sensitiveEmail);
    }

    private static string ExtractToken(string redactedText)
    {
        int start = redactedText.IndexOf("{{", StringComparison.Ordinal);
        int end = redactedText.IndexOf("}}", StringComparison.Ordinal);

        return start >= 0 && end > start
            ? redactedText[start..(end + 2)]
            : string.Empty;
    }
}
