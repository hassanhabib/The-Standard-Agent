// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Services.Foundations.Memorys;
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
        var memoryService = new Mock<IMemoryService>();
        var rememberTool = new RememberTool(memoryService.Object);

        // when
        string actualResult = await rememberTool.ExecuteAsync(fact);

        // then
        actualResult.Should().Contain(fact);

        memoryService.Verify(service =>
            service.RememberAsync(fact),
                Times.Once);
    }

    [Fact]
    public async Task ShouldExtractFactFromStructuredArgumentsOnExecuteAsync()
    {
        // given
        var memoryService = new Mock<IMemoryService>();
        var rememberTool = new RememberTool(memoryService.Object);

        // when
        await rememberTool.ExecuteAsync("{\"fact\":\"Paris is the capital of France\"}");

        // then
        memoryService.Verify(service =>
            service.RememberAsync("Paris is the capital of France"),
                Times.Once);
    }
}
