// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Brokers.Sessions;
using Standard.Agents.Models.Foundations.Sessions.Exceptions;

namespace Standard.Agents.Services.Foundations.Sessions;

public partial class SessionService
{
    private static void ValidateSessionId(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new InvalidSessionException(
                message: "Invalid session. Please correct the error and try again.");
        }
    }

    // A session with no id cannot be read back, so writing one loses the conversation silently —
    // the next prompt finds nothing and the agent forgets, with no error anywhere to explain it.
    private static void ValidateSession(AgentSession session)
    {
        if (session is null || string.IsNullOrWhiteSpace(session.Id))
        {
            throw new InvalidSessionException(
                message: "Invalid session. Please correct the error and try again.");
        }
    }
}
