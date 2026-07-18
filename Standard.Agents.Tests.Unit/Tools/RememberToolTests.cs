// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Brokers.Memorys;
using Standard.Agents.Tools;
using Xunit;

namespace Standard.Agents.Tests.Unit.Tools;

public class RememberToolTests
{
    [Fact]
    public async Task ShouldWriteToMemoryOnExecuteAsync()
    {
        // given
        string fact = "Hassan works on PeerLLM";
        var memoryBroker = new Mock<IMemoryBroker>();
        var rememberTool = new RememberTool(memoryBroker.Object);

        // when
        string actualResult = await rememberTool.ExecuteAsync(fact);

        // then
        actualResult.Should().Contain(fact);

        memoryBroker.Verify(broker =>
            broker.InsertMemoryAsync(fact),
                Times.Once);
    }

    [Fact]
    public async Task ShouldExtractFactFromStructuredArgumentsOnExecuteAsync()
    {
        // given
        var memoryBroker = new Mock<IMemoryBroker>();
        var rememberTool = new RememberTool(memoryBroker.Object);

        // when
        await rememberTool.ExecuteAsync("{\"fact\":\"Paris is the capital of France\"}");

        // then
        memoryBroker.Verify(broker =>
            broker.InsertMemoryAsync("Paris is the capital of France"),
                Times.Once);
    }
}
