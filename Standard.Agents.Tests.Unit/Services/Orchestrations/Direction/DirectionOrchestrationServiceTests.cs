// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Linq.Expressions;
using Moq;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Services.Foundations.ExternalTools;
using Standard.Agents.Services.Foundations.InternalTools;
using Standard.Agents.Services.Foundations.Returns;
using Standard.Agents.Services.Orchestrations.Direction;
using Tynamix.ObjectFiller;
using Xeptions;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Orchestrations.Direction;

public partial class DirectionOrchestrationServiceTests
{
    private readonly Mock<IInternalToolService> internalToolServiceMock;
    private readonly Mock<IExternalToolService> externalToolServiceMock;
    private readonly Mock<IReturnService> returnServiceMock;
    private readonly Mock<ILoggingBroker> loggingBrokerMock;
    private readonly IDirectionOrchestrationService directionOrchestrationService;

    public DirectionOrchestrationServiceTests()
    {
        this.internalToolServiceMock = new Mock<IInternalToolService>();
        this.externalToolServiceMock = new Mock<IExternalToolService>();
        this.returnServiceMock = new Mock<IReturnService>();
        this.loggingBrokerMock = new Mock<ILoggingBroker>();

        this.directionOrchestrationService = new DirectionOrchestrationService(
            internalToolService: this.internalToolServiceMock.Object,
            externalToolService: this.externalToolServiceMock.Object,
            returnService: this.returnServiceMock.Object,
            loggingBroker: this.loggingBrokerMock.Object);
    }

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
