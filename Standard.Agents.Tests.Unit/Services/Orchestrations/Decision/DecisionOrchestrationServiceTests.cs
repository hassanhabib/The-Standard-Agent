// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Linq.Expressions;
using Moq;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Services.Foundations.Decision;
using Standard.Agents.Services.Orchestrations.Decision;
using Tynamix.ObjectFiller;
using Xeptions;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Orchestrations.Decision;

public partial class DecisionOrchestrationServiceTests
{
    private readonly Mock<IGateService> gateServiceMock;
    private readonly Mock<IBrainService> brainServiceMock;
    private readonly Mock<IJudgeService> judgeServiceMock;
    private readonly Mock<ILoggingBroker> loggingBrokerMock;
    private readonly IDecisionOrchestrationService decisionOrchestrationService;

    public DecisionOrchestrationServiceTests()
    {
        this.gateServiceMock = new Mock<IGateService>();
        this.brainServiceMock = new Mock<IBrainService>();
        this.judgeServiceMock = new Mock<IJudgeService>();
        this.loggingBrokerMock = new Mock<ILoggingBroker>();

        this.decisionOrchestrationService = new DecisionOrchestrationService(
            gateService: this.gateServiceMock.Object,
            brainService: this.brainServiceMock.Object,
            judgeService: this.judgeServiceMock.Object,
            loggingBroker: this.loggingBrokerMock.Object);
    }

    private static string CreateRandomString() =>
        new MnemonicString().GetValue();

    private static AgentContext CreateRandomAgentContext() =>
        new()
        {
            Prompt = CreateRandomString(),
            SystemPrompt = CreateRandomString()
        };

    // Gate allows, so the Brain is reached. Used by every test that is not about
    // the Gate itself.
    private void SetupGateAllows() =>
        this.gateServiceMock.Setup(service =>
            service.ScreenAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("allow");

    private void SetupJudgeApproves() =>
        this.judgeServiceMock.Setup(service =>
            service.EvaluateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(1.0);

    private static Expression<Func<Xeption, bool>> SameExceptionAs(Xeption expectedException) =>
        actualException => actualException.SameExceptionAs(expectedException);

    // Exceptions from any of Decision's three foundations, unified into one
    // orchestration category.
    public static TheoryData<Xeption> DependencyValidationExceptions() =>
        new()
        {
            new Models.Foundations.Gates.Exceptions.GateValidationException(
                "gate validation", new Xeption("inner")),

            new Models.Foundations.Brains.Exceptions.BrainValidationException(
                "brain validation", new Xeption("inner")),

            new Models.Foundations.Judges.Exceptions.JudgeValidationException(
                "judge validation", new Xeption("inner")),

            new Models.Foundations.Brains.Exceptions.BrainDependencyValidationException(
                "brain dependency validation", new Xeption("inner"))
        };

    public static TheoryData<Xeption> DependencyExceptions() =>
        new()
        {
            new Models.Foundations.Gates.Exceptions.GateDependencyException(
                "gate dependency", new Xeption("inner")),

            new Models.Foundations.Gates.Exceptions.GateServiceException(
                "gate service", new Xeption("inner")),

            new Models.Foundations.Brains.Exceptions.BrainDependencyException(
                "brain dependency", new Xeption("inner")),

            new Models.Foundations.Judges.Exceptions.JudgeDependencyException(
                "judge dependency", new Xeption("inner"))
        };
}
