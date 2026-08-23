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
        List<string> remembered = [];

        var rememberTool = new RememberTool(memory =>
        {
            remembered.Add(memory);

            return ValueTask.CompletedTask;
        });

        // when
        string actualResult = await rememberTool.ExecuteAsync(fact);

        // then
        actualResult.Should().Contain(fact);
        remembered.Should().ContainSingle().Which.Should().Be(fact);
    }

    [Fact]
    public async Task ShouldExtractFactFromStructuredArgumentsOnExecuteAsync()
    {
        // given
        List<string> remembered = [];

        var rememberTool = new RememberTool(memory =>
        {
            remembered.Add(memory);

            return ValueTask.CompletedTask;
        });

        // when
        await rememberTool.ExecuteAsync("{\"fact\":\"Paris is the capital of France\"}");

        // then
        remembered.Should().ContainSingle().Which.Should().Be("Paris is the capital of France");
    }

    // The converting alias keeps working — the same guarantee the LocalBrain → OnBrain aliases
    // carry — but nothing of the service survives into the tool: it accepts the service and
    // keeps only its routine.
    [Fact]
    public async Task ShouldKeepTheObsoleteServiceConstructorBehavingAsync()
    {
        // given
        var memoryService = new Mock<IMemoryService>();

#pragma warning disable CS0618 // the alias is exactly what is under test
        var rememberTool = new RememberTool(memoryService.Object);
#pragma warning restore CS0618

        // when
        await rememberTool.ExecuteAsync("a fact worth keeping");

        // then
        memoryService.Verify(service =>
            service.RememberAsync("a fact worth keeping"),
                Times.Once);
    }
}
