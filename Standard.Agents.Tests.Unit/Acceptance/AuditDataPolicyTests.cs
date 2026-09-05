// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Brokers.Knowledges;
using Standard.Agents.Brokers.Memorys;
using Standard.Agents.Brokers.Skills;
using Standard.Agents.Models.Foundations.Brains;
using Standard.Agents.Models.Foundations.Skills;
using Standard.Agents.Tools;
using Xunit;

namespace Standard.Agents.Tests.Unit.Acceptance;

// Found in the 2026-09-04 principal review (F-14): Redact protected the model boundary, not the
// logging boundary. The decision log kept the raw prompt, the system prompt, the Brain's reply
// and every tool payload in clear text — and an audit sink usually has broader access and a
// longer life than anything at runtime. The decision log now records METADATA by default: what
// happened, who did it, when, and how large and which payload it was (length and hash), never
// the payload itself. Payload capture is a separate, deliberate opt-in, and when it is on, the
// configured redaction applies at the audit boundary as it does at the model boundary.
public class AuditDataPolicyTests : IDisposable
{
    private const string CardNumber = "4111-1111-1111-1111";

    private readonly string auditPath =
        Path.Combine(Path.GetTempPath(), $"audit-policy-{Guid.NewGuid():n}.jsonl");

    public void Dispose()
    {
        if (File.Exists(this.auditPath))
        {
            File.Delete(this.auditPath);
        }
    }

    // The sink is JSON lines, and the serializer escapes an apostrophe as a ' sequence;
    // read it the way a reader would, with the escape undone, so an assertion can name a tool.
    private async Task<string> ReadDecisionLogAsync() =>
        (await File.ReadAllTextAsync(this.auditPath)).Replace("\\u0027", "'");

    // A tool the Brain calls with the card number, so a tool payload and a tool result both
    // carry it; a Brain that echoes it back, so the reply carries it; a prompt that starts it.
    private sealed class EchoTool : ITool
    {
        public string Name => "echo";

        public string Description => "Echoes its input.";

        public async ValueTask<string> ExecuteAsync(string input) => $"echoed {input}";
    }

    private static StandardAgent BareShell()
    {
        var skillBroker = new Mock<ISkillBroker>();
        skillBroker.Setup(broker => broker.SelectSkillsAsync()).ReturnsAsync(new List<Skill>());

        var memoryBroker = new Mock<IMemoryBroker>();
        memoryBroker.Setup(broker => broker.SelectMemoriesAsync()).ReturnsAsync([]);

        var knowledgeBroker = new Mock<IKnowledgeBroker>();

        knowledgeBroker.Setup(broker => broker.SelectKnowledgeAsync(It.IsAny<string>()))
            .ReturnsAsync([]);

        return new StandardAgent()
            .UseSkills(skillBroker.Object)
            .UseMemory(memoryBroker.Object)
            .UseKnowledge(knowledgeBroker.Object);
    }

    private StandardAgent EchoingAgent()
    {
        int turn = 0;

        return BareShell()
            .Tool(new EchoTool())
            .Audit(this.auditPath)
            .OnBrain(async (systemPrompt, userPrompt) =>
                ++turn is 1
                    ? $"ACTION: echo: {CardNumber}"
                    : $"FINAL: noted {CardNumber}");
    }

    [Fact]
    public async Task ShouldWithholdPayloadsFromTheDecisionLogByDefaultAsync()
    {
        // given
        StandardAgent agent = EchoingAgent();

        // when
        string answer = await agent.ProcessPromptAsync($"my card is {CardNumber}");
        string decisionLog = await ReadDecisionLogAsync();

        // then — the run happened and the answer carried the number; the log did not
        answer.Should().Contain(CardNumber);
        decisionLog.Should().NotContain(CardNumber);

        // and what happened is still on the record: the events, and the shape of the payloads
        decisionLog.Should().Contain("Received prompt");
        decisionLog.Should().Contain("Brain replied");
        decisionLog.Should().Contain("Tool 'echo' input");
        decisionLog.Should().Contain("Tool 'echo' output");
        decisionLog.Should().Contain("\"payloadLength\":");
        decisionLog.Should().Contain("\"payloadHash\":");
    }

    [Fact]
    public async Task ShouldRedactPayloadsInTheDecisionLogWhenRedactionIsConfiguredAsync()
    {
        // given — payload capture on, and a redaction rule for card numbers
        var cardRule = new RedactionRule
        {
            Label = "CARD",
            Pattern = @"\d{4}-\d{4}-\d{4}-\d{4}"
        };

        StandardAgent agent = EchoingAgent()
            .Redact(cardRule)
            .AuditPayloads();

        // when
        await agent.ProcessPromptAsync($"my card is {CardNumber}");
        string decisionLog = await ReadDecisionLogAsync();

        // then — the payloads are on the record, tokenized the way the Brain saw them
        decisionLog.Should().NotContain(CardNumber);
        decisionLog.Should().Contain("CARD");
        decisionLog.Should().Contain("\"payload\":");
    }
}
