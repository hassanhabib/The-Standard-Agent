// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Models.Brokers.Sessions;
using Standard.Agents.Services.Foundations.Memorys;
using Standard.Agents.Services.Foundations.Sessions;

namespace Standard.Agents.Services.Orchestrations.Data.Recollections;

public partial class RecollectionOrchestrationService : IRecollectionOrchestrationService
{
    private readonly IMemoryService memoryService;
    private readonly ISessionService sessionService;
    private readonly ILoggingBroker loggingBroker;

    public RecollectionOrchestrationService(
        IMemoryService memoryService,
        ISessionService sessionService,
        ILoggingBroker loggingBroker)
    {
        this.memoryService = memoryService;
        this.sessionService = sessionService;
        this.loggingBroker = loggingBroker;
    }

    public ValueTask<IReadOnlyList<string>> RecallMemoriesAsync() =>
    TryCatch(async () =>
    {
        IReadOnlyList<string> memories = await this.memoryService.RecallMemoriesAsync();

        await this.loggingBroker.LogProcessAsync("Data", $"Recalled {memories.Count} memories");

        return memories;
    });

    public ValueTask RememberAsync(string memory) =>
        this.memoryService.RememberAsync(memory);

    // Loading a conversation IS recall — which is why the session belongs to Data and not to the
    // loop that used to hold its broker (docs/architecture-alignment.md).
    public ValueTask<AgentSession?> RecallSessionAsync(string sessionId) =>
        this.sessionService.RetrieveSessionAsync(sessionId);

    public ValueTask RecordSessionAsync(AgentSession session) =>
        this.sessionService.RecordSessionAsync(session);
}
