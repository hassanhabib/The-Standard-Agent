// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Brokers.Audits;
using Standard.Agents.Brokers.Knowledges;
using Standard.Agents.Brokers.Memorys;
using Standard.Agents.Brokers.Skills;
using Standard.Agents.Models.Brokers.Audits;
using Standard.Agents.Models.Foundations.Skills;
using Xunit;

namespace Standard.Agents.Tests.Unit.Clients;

// The decision log's three access modes (SPEC.md §4.8): Local is .Audit(path) and is
// covered by the acceptance suite, since a file sink is only interesting end to end.
// External and Custom are covered here, where the sink can be observed directly.
public class StandardAgentAuditTests
{
    private static StandardAgent AnsweringAgent()
    {
        var skillBroker = new Mock<ISkillBroker>();

        skillBroker.Setup(broker => broker.SelectSkillsAsync())
            .ReturnsAsync(new List<Skill>());

        var memoryBroker = new Mock<IMemoryBroker>();
        memoryBroker.Setup(broker => broker.SelectMemoriesAsync()).ReturnsAsync([]);

        var knowledgeBroker = new Mock<IKnowledgeBroker>();

        knowledgeBroker.Setup(broker => broker.SelectKnowledgeAsync(It.IsAny<string>()))
            .ReturnsAsync([]);

        return new StandardAgent()
            .UseSkills(skillBroker.Object)
            .UseMemory(memoryBroker.Object)
            .UseKnowledge(knowledgeBroker.Object)
            .OnBrain(async (systemPrompt, userPrompt) => "FINAL: 42");
    }

    [Fact]
    public async Task ShouldWriteToExternalAuditBrokerOnProcessPromptAsync()
    {
        // given
        var auditBroker = new Mock<IAuditBroker>();

        StandardAgent agent = AnsweringAgent()
            .UseAudit(auditBroker.Object);

        // when
        await agent.ProcessPromptAsync(prompt: "what is the answer?");

        // then
        auditBroker.Verify(broker =>
            broker.WriteAsync(It.IsAny<AuditRecord>()),
                Times.AtLeastOnce);
    }

}
