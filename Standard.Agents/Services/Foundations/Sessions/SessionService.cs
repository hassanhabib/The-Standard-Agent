// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Brokers.Sessions;
using Standard.Agents.Models.Brokers.Sessions;

namespace Standard.Agents.Services.Foundations.Sessions;

// The conversation, as a foundation rather than as a broker reached from above.
//
// Coordination held ISessionBroker directly, which meant a disk full or a permission denied
// surfaced unmapped and was attributed to the loop rather than to the store. A foundation is what
// supplies the three things that call was missing: validation, exception mapping, and attribution
// (docs/architecture-alignment.md).
public partial class SessionService : ISessionService
{
    private readonly ISessionBroker sessionBroker;
    private readonly ILoggingBroker loggingBroker;

    public SessionService(
        ISessionBroker sessionBroker,
        ILoggingBroker loggingBroker)
    {
        this.sessionBroker = sessionBroker;
        this.loggingBroker = loggingBroker;
    }

    public ValueTask<AgentSession?> RetrieveSessionAsync(string sessionId) =>
    TryCatch(async () =>
    {
        await ValueTask.CompletedTask;

        return (AgentSession?)null;
    });

    public ValueTask RecordSessionAsync(AgentSession session) =>
    TryCatch(async () =>
    {
        await ValueTask.CompletedTask;
    });
}
