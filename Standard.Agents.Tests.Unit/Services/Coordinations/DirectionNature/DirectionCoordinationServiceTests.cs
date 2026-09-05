// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Linq.Expressions;
using Moq;
using Standard.Agents.Brokers.Times;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Services.Foundations.ExternalTools;
using Standard.Agents.Services.Foundations.InternalTools;
using Standard.Agents.Services.Foundations.Returns;
using Standard.Agents.Brokers.Approvals;
using Standard.Agents.Brokers.Effects;
using Standard.Agents.Brokers.Policies;
using Standard.Agents.Services.Coordinations.Direction;
using Standard.Agents.Services.Foundations.Approvals;
using Standard.Agents.Services.Foundations.EffectLedgers;
using Standard.Agents.Services.Foundations.Policys;
using Standard.Agents.Services.Orchestrations.Direction.Executions;
using Standard.Agents.Services.Orchestrations.Direction.Perimeters;
using Tynamix.ObjectFiller;
using Xeptions;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Coordinations.DirectionNature;

public partial class DirectionCoordinationServiceTests
{
    private readonly Mock<IInternalToolService> internalToolServiceMock;
    private readonly Mock<IExternalToolService> externalToolServiceMock;
    private readonly Mock<IReturnService> returnServiceMock;
    private readonly Mock<ILoggingBroker> loggingBrokerMock;
    private readonly IDirectionCoordinationService directionCoordinationService;

    public DirectionCoordinationServiceTests()
    {
        this.internalToolServiceMock = new Mock<IInternalToolService>();
        this.externalToolServiceMock = new Mock<IExternalToolService>();
        this.returnServiceMock = new Mock<IReturnService>();
        this.loggingBrokerMock = new Mock<ILoggingBroker>();

        // Real regions over mocked FOUNDATIONS - the mocks sit where they sat before
        // regionalization, so every assertion below is unchanged.
        this.directionCoordinationService = new DirectionCoordinationService(
            perimeterService: NewPerimeter(),
            executionService: NewExecution(),
            loggingBroker: this.loggingBrokerMock.Object);
    }

    private PerimeterOrchestrationService NewPerimeter() =>
        new(
            new PolicyService(new NotConfiguredPolicyBroker(), this.loggingBrokerMock.Object),
            new ApprovalService(new NotConfiguredApprovalBroker(), this.loggingBrokerMock.Object),
            new EffectLedgerService(
                new InMemoryEffectLedgerBroker(), new TimeBroker(), this.loggingBrokerMock.Object),
            new TimeBroker(),
            this.loggingBrokerMock.Object);

    private ExecutionOrchestrationService NewExecution() =>
        new(
            this.internalToolServiceMock.Object,
            this.externalToolServiceMock.Object,
            this.returnServiceMock.Object,
            this.loggingBrokerMock.Object);

    private static string CreateRandomString() =>
        new MnemonicString().GetValue();

    private static AgentContext CreateContextWithDirection(
        string directionType,
        string payload) =>
        new()
        {
            Prompt = CreateRandomString(),
            DirectionType = directionType,
            Payload = payload
        };

    private static Expression<Func<Xeption, bool>> SameExceptionAs(Xeption expectedException) =>
        actualException => actualException.SameExceptionAs(expectedException);

    public static TheoryData<Xeption> DependencyValidationExceptions() =>
        new()
        {
            new Models.Foundations.InternalTools.Exceptions.InternalToolValidationException(
                message: "internal tool validation",
                innerException: new Xeption(message: "inner")),

            new Models.Foundations.ExternalTools.Exceptions.ExternalToolValidationException(
                message: "external tool validation",
                innerException: new Xeption(message: "inner"))
        };

    public static TheoryData<Xeption> DependencyExceptions() =>
        new()
        {
            new Models.Foundations.InternalTools.Exceptions.InternalToolDependencyException(
                message: "internal tool dependency",
                innerException: new Xeption(message: "inner")),

            new Models.Foundations.InternalTools.Exceptions.InternalToolServiceException(
                message: "internal tool service",
                innerException: new Xeption(message: "inner")),

            new Models.Foundations.ExternalTools.Exceptions.ExternalToolDependencyException(
                message: "external tool dependency",
                innerException: new Xeption(message: "inner")),

            new Models.Foundations.Returns.Exceptions.ReturnServiceException(
                message: "return service",
                innerException: new Xeption(message: "inner"))
        };
}
