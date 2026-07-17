// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Linq.Expressions;
using Moq;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Brokers.Logs;
using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Services.Coordinations;
using Standard.Agents.Services.Orchestrations.Data;
using Standard.Agents.Services.Orchestrations.Decision;
using Standard.Agents.Services.Orchestrations.Direction;
using Tynamix.ObjectFiller;
using Xeptions;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Coordinations;

public partial class AgentCoordinationServiceTests
{
    private readonly Mock<IDataOrchestrationService> dataOrchestrationServiceMock;
    private readonly Mock<IDecisionOrchestrationService> decisionOrchestrationServiceMock;
    private readonly Mock<IDirectionOrchestrationService> directionOrchestrationServiceMock;
    private readonly Mock<ILogBroker> logBrokerMock;
    private readonly Mock<ILoggingBroker> loggingBrokerMock;
    private readonly IAgentCoordinationService agentCoordinationService;

    public AgentCoordinationServiceTests()
    {
        this.dataOrchestrationServiceMock = new Mock<IDataOrchestrationService>();
        this.decisionOrchestrationServiceMock = new Mock<IDecisionOrchestrationService>();
        this.directionOrchestrationServiceMock = new Mock<IDirectionOrchestrationService>();
        this.logBrokerMock = new Mock<ILogBroker>();
        this.loggingBrokerMock = new Mock<ILoggingBroker>();

        this.agentCoordinationService = new AgentCoordinationService(
            dataOrchestrationService: this.dataOrchestrationServiceMock.Object,
            decisionOrchestrationService: this.decisionOrchestrationServiceMock.Object,
            directionOrchestrationService: this.directionOrchestrationServiceMock.Object,
            logBroker: this.logBrokerMock.Object,
            loggingBroker: this.loggingBrokerMock.Object);
    }

    private static string CreateRandomString() =>
        new MnemonicString().GetValue();

    private void SetupOrchestrationsPassThrough()
    {
        this.dataOrchestrationServiceMock.Setup(service =>
            service.RecallAsync(It.IsAny<AgentContext>()))
                .ReturnsAsync((AgentContext context) => context);

        this.decisionOrchestrationServiceMock.Setup(service =>
            service.ThinkAsync(It.IsAny<AgentContext>()))
                .ReturnsAsync((AgentContext context) => context);
    }

    private void SetupDirectionTerminates(string result) =>
        this.directionOrchestrationServiceMock.Setup(service =>
            service.ActAsync(It.IsAny<AgentContext>()))
                .ReturnsAsync((AgentContext context) =>
                    context with { Result = result, Status = AgentStatus.Responded });

    private void SetupDirectionNeverTerminates(string result) =>
    this.directionOrchestrationServiceMock.Setup(service =>
        service.ActAsync(It.IsAny<AgentContext>()))
            .ReturnsAsync((AgentContext context) =>
                context with { Result = result, Status = AgentStatus.Working });

    private static Expression<Func<Xeption, bool>> SameExceptionAs(Xeption expectedException) =>
        actualException => actualException.SameExceptionAs(expectedException);

    public static TheoryData<Xeption> DependencyExceptions() =>
        new()
        {
            new Models.Orchestrations.Agents.Exceptions.AgentOrchestrationDependencyException(
                message: "orchestration dependency",
                innerException: new Xeption(message: "inner")),

            new Models.Orchestrations.Agents.Exceptions.AgentOrchestrationServiceException(
                message: "orchestration service",
                innerException: new Xeption(message: "inner"))
        };
        }
