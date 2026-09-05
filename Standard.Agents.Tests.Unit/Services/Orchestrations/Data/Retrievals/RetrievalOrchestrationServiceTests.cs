// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Moq;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Services.Foundations.ExternalTools;
using Standard.Agents.Services.Foundations.Knowledges;
using Standard.Agents.Services.Foundations.Skills;
using Standard.Agents.Services.Orchestrations.Data.Retrievals;
using Tynamix.ObjectFiller;

namespace Standard.Agents.Tests.Unit.Services.Orchestrations.Data.Retrievals;

// Retrieval composes what the Brain is shown: the skills, and the catalog of tools it may reach
// for. The catalog is a safety boundary, so how remote tools join it — and what happens when
// their server cannot be asked — is orchestration behavior worth pinning on its own.
public partial class RetrievalOrchestrationServiceTests
{
    private const string LocalToolCatalog = "- calculator — Evaluates arithmetic. parameters: {}";

    private readonly Mock<ISkillService> skillServiceMock;
    private readonly Mock<IKnowledgeService> knowledgeServiceMock;
    private readonly Mock<IExternalToolService> externalToolServiceMock;
    private readonly Mock<ILoggingBroker> loggingBrokerMock;
    private readonly IRetrievalOrchestrationService retrievalOrchestrationService;

    public RetrievalOrchestrationServiceTests()
    {
        this.skillServiceMock = new Mock<ISkillService>();
        this.knowledgeServiceMock = new Mock<IKnowledgeService>();
        this.externalToolServiceMock = new Mock<IExternalToolService>();
        this.loggingBrokerMock = new Mock<ILoggingBroker>();

        this.retrievalOrchestrationService = new RetrievalOrchestrationService(
            skillService: this.skillServiceMock.Object,
            knowledgeService: this.knowledgeServiceMock.Object,
            toolCatalog: LocalToolCatalog,
            loggingBroker: this.loggingBrokerMock.Object,
            externalToolService: this.externalToolServiceMock.Object);
    }

    private static string CreateRandomString() =>
        new MnemonicString().GetValue();
}
