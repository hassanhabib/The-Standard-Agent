// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Xeptions;

namespace Standard.Agents.Models.Clients.Agents.Exceptions;

/// <summary>
/// A budget the agent cannot honestly enforce — a cost bound with no rate to price it. Spend is
/// the token count times the rate, so a dollar bound priced at zero is zero dollars forever: a
/// guardrail that looks armed and never trips. The framework cannot know what a model costs, so
/// it refuses the contradiction rather than composing it silently.
/// </summary>
public class InvalidAgentBudgetException : Xeption
{
    public InvalidAgentBudgetException(string message)
        : base(message)
    { }
}
