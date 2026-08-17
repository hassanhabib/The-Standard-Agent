// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Linq.Expressions;
using Moq;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Services.Foundations.Knowledges;
using Standard.Agents.Services.Foundations.Memorys;
using Standard.Agents.Services.Foundations.Skills;
using Standard.Agents.Services.Coordinations.Data;
using Standard.Agents.Services.Foundations.Sessions;
using Standard.Agents.Services.Orchestrations.Data.Recollections;
using Standard.Agents.Services.Orchestrations.Data.Retrievals;
using Tynamix.ObjectFiller;
using Xeptions;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Coordinations.DataNature;

public partial class DataCoordinationServiceTests
{
    private const string ToolCatalog = "- calculator — Evaluate arithmetic. parameters: {}";

    private readonly Mock<ISkillService> skillServiceMock;
    private readonly Mock<IMemoryService> memoryServiceMock;
    private readonly Mock<IKnowledgeService> knowledgeServiceMock;
    private readonly Mock<ILoggingBroker> loggingBrokerMock;
    private readonly IDataCoordinationService dataCoordinationService;

    public DataCoordinationServiceTests()
    {
        this.skillServiceMock = new Mock<ISkillService>();
        this.memoryServiceMock = new Mock<IMemoryService>();
        this.knowledgeServiceMock = new Mock<IKnowledgeService>();
        this.loggingBrokerMock = new Mock<ILoggingBroker>();

        this.knowledgeServiceMock.Setup(service =>
            service.RetrieveKnowledgeAsync(It.IsAny<string>()))
                .ReturnsAsync([]);

        // The regions are real; the mocks stay at the FOUNDATION level, which is where they
        // were before regionalization. Every assertion below is therefore unchanged.
        this.dataCoordinationService = new DataCoordinationService(
            retrievalService: new RetrievalOrchestrationService(
                skillService: this.skillServiceMock.Object,
                knowledgeService: this.knowledgeServiceMock.Object,
                toolCatalog: ToolCatalog,
                loggingBroker: this.loggingBrokerMock.Object),
            recollectionService: new RecollectionOrchestrationService(
                memoryService: this.memoryServiceMock.Object,
                sessionService: new Mock<ISessionService>().Object,
                loggingBroker: this.loggingBrokerMock.Object),
            loggingBroker: this.loggingBrokerMock.Object);
    }

    private static string CreateRandomString() =>
        new MnemonicString().GetValue();

    private static AgentContext CreateRandomAgentContext() =>
        new() { Prompt = CreateRandomString() };

    private static Expression<Func<Xeption, bool>> SameExceptionAs(Xeption expectedException) =>
        actualException => actualException.SameExceptionAs(expectedException);

    public static TheoryData<Xeption> DependencyValidationExceptions() =>
        new()
        {
            new Models.Foundations.Memorys.Exceptions.MemoryValidationException(
                message: "memory validation",
                innerException: new Xeption(message: "inner")),

            new Models.Foundations.Knowledges.Exceptions.KnowledgeValidationException(
                message: "knowledge validation",
                innerException: new Xeption(message: "inner"))
        };

    public static TheoryData<Xeption> DependencyExceptions() =>
        new()
        {
            new Models.Foundations.Skills.Exceptions.SkillDependencyException(
                message: "skill dependency",
                innerException: new Xeption(message: "inner")),

            new Models.Foundations.Skills.Exceptions.SkillServiceException(
                message: "skill service",
                innerException: new Xeption(message: "inner")),

            new Models.Foundations.Memorys.Exceptions.MemoryDependencyException(
                message: "memory dependency",
                innerException: new Xeption(message: "inner")),

            new Models.Foundations.Knowledges.Exceptions.KnowledgeDependencyException(
                message: "knowledge dependency",
                innerException: new Xeption(message: "inner"))
        };
}
