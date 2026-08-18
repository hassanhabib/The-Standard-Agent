// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Linq.Expressions;
using Moq;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Models.Foundations.Judges;
using Standard.Agents.Models.Foundations.Usages;
using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Services.Foundations.Brains;
using Standard.Agents.Services.Foundations.Gates;
using Standard.Agents.Services.Foundations.Judges;
using Standard.Agents.Services.Foundations.Usages;
using Standard.Agents.Services.Coordinations.Decision;
using Standard.Agents.Services.Orchestrations.Decision.Guardians;
using Standard.Agents.Services.Orchestrations.Decision.Inferences;
using Tynamix.ObjectFiller;
using Xeptions;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Coordinations.DecisionNature;

public partial class DecisionCoordinationServiceTests
{
    private readonly Mock<IGateService> gateServiceMock;
    private readonly Mock<IBrainService> brainServiceMock;
    private readonly Mock<IJudgeService> judgeServiceMock;
    private readonly Mock<ILoggingBroker> loggingBrokerMock;
    private readonly Mock<IUsageService> usageServiceMock;
    private readonly IDecisionCoordinationService decisionCoordinationService;

    public DecisionCoordinationServiceTests()
    {
        this.gateServiceMock = new Mock<IGateService>();
        this.brainServiceMock = new Mock<IBrainService>();
        this.judgeServiceMock = new Mock<IJudgeService>();
        this.loggingBrokerMock = new Mock<ILoggingBroker>();
        this.usageServiceMock = new Mock<IUsageService>();

        // Measurement is not what these tests are about, so it answers "nothing consumed" and
        // stays out of the way. What it must NOT do is return a default ValueTask, which would
        // fault every path through Inference for a reason unrelated to the assertion.
        this.usageServiceMock.Setup(service =>
            service.MeasureAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(AgentUsage.None);

        // Real regions over mocked FOUNDATIONS - the mocks sit where they sat before
        // regionalization, so every assertion below is unchanged.
        this.decisionCoordinationService = new DecisionCoordinationService(
            inferenceService: new InferenceOrchestrationService(
                this.brainServiceMock.Object,
                this.usageServiceMock.Object,
                this.loggingBrokerMock.Object),
            guardianService: new GuardianOrchestrationService(
                this.gateServiceMock.Object,
                this.judgeServiceMock.Object,
                this.loggingBrokerMock.Object),
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

    private void SetupGateAllows() =>
        this.gateServiceMock.Setup(service =>
    service.ScreenAsync(It.IsAny<string>()))
        .ReturnsAsync("allow");

    private void SetupJudgeApproves() =>
        this.judgeServiceMock.Setup(service =>
            service.EvaluateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new Judgement { Score = 1.0 });

    private static Expression<Func<Xeption, bool>> SameExceptionAs(Xeption expectedException) =>
        actualException => actualException.SameExceptionAs(expectedException);

    public static TheoryData<Xeption> DependencyValidationExceptions() =>
        new()
        {
            new Models.Foundations.Gates.Exceptions.GateValidationException(
                message: "gate validation",
                innerException: new Xeption(message: "inner")),

            new Models.Foundations.Brains.Exceptions.BrainValidationException(
                message: "brain validation",
                innerException: new Xeption(message: "inner")),

            new Models.Foundations.Judges.Exceptions.JudgeValidationException(
                message: "judge validation",
                innerException: new Xeption(message: "inner")),

            new Models.Foundations.Brains.Exceptions.BrainDependencyValidationException(
                message: "brain dependency validation",
                innerException: new Xeption(message: "inner"))
        };

    public static TheoryData<Xeption> DependencyExceptions() =>
        new()
        {
            new Models.Foundations.Gates.Exceptions.GateDependencyException(
                message: "gate dependency",
                innerException: new Xeption(message: "inner")),

            new Models.Foundations.Gates.Exceptions.GateServiceException(
                message: "gate service",
                innerException: new Xeption(message: "inner")),

            new Models.Foundations.Brains.Exceptions.BrainDependencyException(
                message: "brain dependency",
                innerException: new Xeption(message: "inner")),

            new Models.Foundations.Judges.Exceptions.JudgeDependencyException(
                message: "judge dependency",
                innerException: new Xeption(message: "inner"))
        };
}
