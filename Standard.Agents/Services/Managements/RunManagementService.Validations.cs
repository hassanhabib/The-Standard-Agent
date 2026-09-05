// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Coordinations.Agents.Exceptions;

namespace Standard.Agents.Services.Managements;

public partial class RunManagementService
{
    private static void ValidatePrompt(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new InvalidAgentException(
                message: "Invalid prompt. Please correct the error and try again.");
        }
    }

    // A session belongs to the principal that opened it. A session with no owner is the
    // anonymous, shared-by-id one that always existed; one with an owner admits that owner and
    // no one else (SPEC.md §4.11; principal review 2026-09-04, F-06).
    private static void ValidateSessionOwner(
        Models.Brokers.Sessions.AgentSession? session,
        string principal)
    {
        if (session is { Owner.Length: > 0 } && session.Owner != principal)
        {
            throw new InvalidAgentException(
                message: "Invalid session. It belongs to another principal; use a session of "
                    + "your own.");
        }
    }
}
