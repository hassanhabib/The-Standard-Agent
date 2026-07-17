// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Linq.Expressions;
using Moq;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Services.Foundations.Data;
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

    // Exceptions from any of the three foundations, unified into one orchestration
    // category. Theory-driven so one test covers all of them, per The Standard.
    //
    // No SkillValidationException here — RetrieveSkillsAsync takes no input, so
    // Skills has no validation category at all. The asymmetry is real, not an
    // oversight; asserting on a type that does not exist would be inventing a
    // failure the system cannot produce.
    public static TheoryData<Xeption> DependencyValidationExceptions() =>
        new()
        {
            new Models.Foundations.Memories.Exceptions.MemoryValidationException(
                "memory validation", new Xeption("inner")),

            new Models.Foundations.Knowledges.Exceptions.KnowledgeValidationException(
                "knowledge validation", new Xeption("inner"))
        };

    public static TheoryData<Xeption> DependencyExceptions() =>
        new()
        {
            new Models.Foundations.Skills.Exceptions.SkillDependencyException(
                "skill dependency", new Xeption("inner")),

            new Models.Foundations.Skills.Exceptions.SkillServiceException(
                "skill service", new Xeption("inner")),

            new Models.Foundations.Memories.Exceptions.MemoryDependencyException(
                "memory dependency", new Xeption("inner")),

            new Models.Foundations.Knowledges.Exceptions.KnowledgeDependencyException(
                "knowledge dependency", new Xeption("inner"))
        };
}
