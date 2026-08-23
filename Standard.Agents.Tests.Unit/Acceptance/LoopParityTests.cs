// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Brokers.Knowledges;
using Standard.Agents.Brokers.Memorys;
using Standard.Agents.Brokers.Skills;
using Standard.Agents.Models.Clients.Agents;
using Standard.Agents.Models.Foundations.Skills;
using Xunit;

namespace Standard.Agents.Tests.Unit.Acceptance;

// One prompt, two doors, one loop. The 2026-08-23 sweep found six controls enforced on one door
// and not the other — screening, the contract, the revision reset, the approval hold's event,
// route capture, native calling — and every one of them was introduced by editing one loop and
// not its twin. The parity tests of that era covered exactly the four controls named when they
// were written, which is why the six hid behind them: an enumerated rule lags the thing it
// governs.
public class LoopParityTests
{
    private static StandardAgent BareAgent(Func<string, string, ValueTask<string>> brain)
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
            .UseKnowledge(knowledgeBroker.Object)
            .OnBrain(brain);
    }

    // A fault is localized once and surfaces in the run-management family, whichever door the
    // prompt entered by. The batched door maps through TryCatch; the streamed door used to hand
    // the raw inner-tier exception to the caller — so error-handling code written against one
    // door silently failed to match the other's types.
    [Fact]
    public async Task ShouldSurfaceTheSameExceptionFamilyOnBothDoorsAsync()
    {
        // given — a brain that faults
        Func<string, string, ValueTask<string>> faultingBrain =
            (_, _) => throw new InvalidOperationException("the model host is down");

        StandardAgent batched = BareAgent(faultingBrain);
        StandardAgent streamed = BareAgent(faultingBrain);

        // when
        Exception? batchedException = await Record.ExceptionAsync(() =>
            batched.ProcessPromptAsync("hello").AsTask());

        Exception? streamedException = await Record.ExceptionAsync(async () =>
        {
            await foreach (AgentStreamEvent _ in streamed.StreamPromptAsync("hello"))
            {
            }
        });

        // then
        batchedException.Should().NotBeNull();
        streamedException.Should().NotBeNull();

        streamedException!.GetType().Should().Be(
            batchedException!.GetType(),
            because: "a caller that cannot rely on the exception family cannot write one "
                + "error handler for both doors, and the mapping is a control like any other");
    }
}
