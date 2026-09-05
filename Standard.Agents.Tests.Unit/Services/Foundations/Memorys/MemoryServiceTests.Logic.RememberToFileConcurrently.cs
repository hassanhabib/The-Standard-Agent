// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Standard.Agents.Brokers.Files;
using Standard.Agents.Services.Foundations.Memorys;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Memorys;

public partial class MemoryServiceTests
{
    // Found in the 2026-09-04 principal review (F-05): the file append was not serialized, so
    // simultaneous remembers — the shape a hosted singleton meets when several callers store a
    // fact at once — could collide on the file and fail, or interleave. Every fact stored must
    // be in the file afterwards, whole and once.
    [Fact]
    public async Task ShouldRememberConcurrentlyToFileWithoutLosingAMemoryAsync()
    {
        // given
        string memoryPath =
            Path.Combine(Path.GetTempPath(), $"standard-agent-memory-{Guid.NewGuid():N}.txt");

        var fileMemoryService = new MemoryService(
            fileBroker: new FileBroker(),
            memoryPath: memoryPath,
            loggingBroker: this.loggingBrokerMock.Object);

        IReadOnlyList<string> memories =
            [.. Enumerable.Range(0, 32).Select(index => $"fact number {index}")];

        try
        {
            // when
            await Task.WhenAll(memories.Select(memory =>
                fileMemoryService.RememberAsync(memory).AsTask()));

            // then
            string[] rememberedMemories = await File.ReadAllLinesAsync(memoryPath);
            rememberedMemories.Should().BeEquivalentTo(memories);
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
        finally
        {
            File.Delete(memoryPath);
        }
    }
}
