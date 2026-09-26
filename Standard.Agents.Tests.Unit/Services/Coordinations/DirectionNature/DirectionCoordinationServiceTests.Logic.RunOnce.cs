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
