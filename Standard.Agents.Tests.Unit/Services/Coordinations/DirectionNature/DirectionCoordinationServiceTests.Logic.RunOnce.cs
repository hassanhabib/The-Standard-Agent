// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Models.Coordinations.Directions;
using Standard.Agents.Models.Loggings;
using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Models.Orchestrations.Effects;
using Standard.Agents.Services.Coordinations.Direction;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Coordinations.DirectionNature;

public partial class DirectionCoordinationServiceTests
{
    [Fact]
    public async Task ShouldLookAgainAtAScopeTheRunHasSinceWrittenToOnActAsync()
    {
        // given
        // Watched in both reference implementations: a model edited a file and read it back to
        // check its edit, and was handed the file as it was before the edit, because the read had
        // the same tool and the same arguments as the read before it (SPEC.md §4.9, v1.14).
        IDirectionCoordinationService scopedService = NewScopedFileService();

        // Inside a run, as every act is: what a run has written is the run's to know.
        using IDisposable run = AgentRun.Begin();

        this.internalToolServiceMock.Setup(service =>
            service.HandlesAsync(It.IsAny<string>())).ReturnsAsync(true);

        this.internalToolServiceMock.SetupSequence(service =>
            service.RunAsync("read_file", "a.txt"))
                .ReturnsAsync("version one")
                .ReturnsAsync("version two");

        this.internalToolServiceMock.Setup(service =>
            service.RunAsync("write_file", "version two")).ReturnsAsync("written");

        AgentContext firstRead = await scopedService.ActAsync(
            CreateContextWithDirection("read_file", "a.txt"));

        AgentContext written = await scopedService.ActAsync(
            firstRead with { DirectionType = "write_file", Payload = "version two" });

        // when
        AgentContext secondRead = await scopedService.ActAsync(
            written with { DirectionType = "read_file", Payload = "a.txt" });

        // then
        // The look after the write is a new question: it runs, and sees what the write left.
        secondRead.Result.Should().Be("version two");

        this.internalToolServiceMock.Verify(service =>
            service.RunAsync("read_file", "a.txt"), Times.Exactly(2));
    }

    [Fact]
    public async Task ShouldLookAgainAtAScopeWrittenToBeforeTheRunWasResumedOnActAsync()
    {
        // given
        // A run read a file, wrote to it, and paused. Resumed, it keeps its identity, so the ledger
        // still holds the read from before the write, and it begins with nothing performed. The
        // writes before the pause are in the conversation it was resumed with, and a count read from
        // the run alone handed the resumed read the file as it was before the write.
        IDirectionCoordinationService scopedService = NewScopedFileService();
        string runId = Guid.NewGuid().ToString("n");

        this.internalToolServiceMock.Setup(service =>
            service.HandlesAsync(It.IsAny<string>())).ReturnsAsync(true);

        this.internalToolServiceMock.SetupSequence(service =>
            service.RunAsync("read_file", "a.txt"))
                .ReturnsAsync("version one")
                .ReturnsAsync("version two");

        this.internalToolServiceMock.Setup(service =>
            service.RunAsync("write_file", "version two")).ReturnsAsync("written");

        AgentContext written;

        using (AgentRun.Begin(runId))
        {
            AgentContext firstRead = await scopedService.ActAsync(
                CreateContextWithDirection("read_file", "a.txt"));

            written = await scopedService.ActAsync(
                firstRead with { DirectionType = "write_file", Payload = "version two" });
        }

        AgentContext resumed = written with
        {
            DirectionType = "read_file",
            Payload = "a.txt",
            ToolExchanges =
            [
                new ToolExchange("call-1", "read_file", "a.txt", "version one"),
                new ToolExchange("call-2", "write_file", "version two", "written")
            ]
        };

        // when
        AgentContext secondRead;

        using (AgentRun.Begin(runId))
        {
            secondRead = await scopedService.ActAsync(resumed);
        }

        // then
        secondRead.Result.Should().Be("version two");

        this.internalToolServiceMock.Verify(service =>
            service.RunAsync("read_file", "a.txt"), Times.Exactly(2));
    }

    [Fact]
    public async Task ShouldTellTheBrainAReplayAlreadyRanOnActAsync()
    {
        // given
        // Handed back bare, a replay reads as a fresh answer to a fresh ask, and a model that asked
        // because it did not have what it wanted asks again (SPEC.md §4.9, v1.14).
        using IDisposable run = AgentRun.Begin();

        this.internalToolServiceMock.Setup(service =>
            service.HandlesAsync("calculator")).ReturnsAsync(true);

        this.internalToolServiceMock.Setup(service =>
            service.RunAsync("calculator", "2 + 2")).ReturnsAsync("4");

        AgentContext first = await this.directionCoordinationService.ActAsync(
            CreateContextWithDirection("calculator", "2 + 2"));

        // when
        AgentContext second = await this.directionCoordinationService.ActAsync(
            first with { DirectionType = "calculator", Payload = "2 + 2" });

        // then
        // The first outcome whole, then the note. The tool ran once.
        second.Result.Should().StartWith("4");
        second.Result.Should().Contain("already ran in this run with the same arguments");

        this.internalToolServiceMock.Verify(service =>
            service.RunAsync("calculator", "2 + 2"), Times.Once);
    }

    [Fact]
    public async Task ShouldMarkAnExchangeTheLedgerAnsweredAsReplayedOnActAsync()
    {
        // given
        // A loop that counts a run's repeated asks has to tell a replay from a call that ran: a read
        // after an edit is the same ask and is not a repeat (SPEC.md §3.2, v1.14).
        using IDisposable run = AgentRun.Begin();

        this.internalToolServiceMock.Setup(service =>
            service.HandlesAsync("calculator")).ReturnsAsync(true);

        this.internalToolServiceMock.Setup(service =>
            service.RunAsync("calculator", "2 + 2")).ReturnsAsync("4");

        AgentContext first = await this.directionCoordinationService.ActAsync(
            CreateContextWithDirection("calculator", "2 + 2") with { ToolCallId = "call_1" });

        // when
        AgentContext second = await this.directionCoordinationService.ActAsync(
            first with { DirectionType = "calculator", Payload = "2 + 2", ToolCallId = "call_2" });

        // then
        second.ToolExchanges.Select(exchange => exchange.Replayed).Should().Equal(false, true);
    }

    [Fact]
    public async Task ShouldAnswerAThirdIdenticalAskWithTheNoteAloneOnActAsync()
    {
        // given
        // Watched live: a page of a file asked for fourteen more times, each answered with the same
        // sixteen kilobytes and the same note. The note was right and was not enough, and every
        // copy cost the person a turn's worth of context (SPEC.md §4.9, v1.14).
        using IDisposable run = AgentRun.Begin();

        this.internalToolServiceMock.Setup(service =>
            service.HandlesAsync("read_file")).ReturnsAsync(true);

        this.internalToolServiceMock.Setup(service =>
            service.RunAsync("read_file", "a.txt")).ReturnsAsync("the whole page");

        AgentContext first = await this.directionCoordinationService.ActAsync(
            CreateContextWithDirection("read_file", "a.txt"));

        AgentContext second = await this.directionCoordinationService.ActAsync(
            first with { DirectionType = "read_file", Payload = "a.txt" });

        // when
        AgentContext third = await this.directionCoordinationService.ActAsync(
            second with { DirectionType = "read_file", Payload = "a.txt" });

        // then
        // Not a third copy of the bytes: the model has had them twice.
        second.Result.Should().StartWith("the whole page");
        third.Result.Should().NotContain("the whole page");
        third.Result.Should().Contain("asked for a third time with the same arguments");
    }

    // A file tool pair over one scope: a Safe read and a Sensitive write, both naming the same file,
    // the way a coding agent's tools do.
    private DirectionCoordinationService NewScopedFileService() =>
        new(
            perimeterService: NewPerimeter(),
            executionService: NewExecution(),
            loggingBroker: this.loggingBrokerMock.Object,
            policy: new PerimeterPolicy
            {
                ToolRisk = new Dictionary<string, RiskLevel>(StringComparer.OrdinalIgnoreCase)
                {
                    ["write_file"] = RiskLevel.Sensitive
                },

                ToolScope = new Dictionary<string, Func<string, string>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["read_file"] = _ => "a.txt",
                    ["write_file"] = _ => "a.txt"
                }
            });
}
