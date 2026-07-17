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
using Standard.Agents.Services.Orchestrations.Data;
using Tynamix.ObjectFiller;
using Xeptions;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Orchestrations.Data;

public partial class DataOrchestrationServiceTests
{
    private readonly Mock<ISkillService> skillServiceMock;
    private readonly Mock<IMemoryService> memoryServiceMock;
    private readonly Mock<IKnowledgeService> knowledgeServiceMock;
    private readonly Mock<ILoggingBroker> loggingBrokerMock;
    private readonly IDataOrchestrationService dataOrchestrationService;

    public DataOrchestrationServiceTests()
    {
        this.skillServiceMock = new Mock<ISkillService>();
        this.memoryServiceMock = new Mock<IMemoryService>();
        this.knowledgeServiceMock = new Mock<IKnowledgeService>();
        this.loggingBrokerMock = new Mock<ILoggingBroker>();

        this.dataOrchestrationService = new DataOrchestrationService(
            skillService: this.skillServiceMock.Object,
            memoryService: this.memoryServiceMock.Object,
            knowledgeService: this.knowledgeServiceMock.Object,
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
