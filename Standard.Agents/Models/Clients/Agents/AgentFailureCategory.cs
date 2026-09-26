// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Models.Clients.Agents;

/// <summary>
/// The categories every failure falls into (SPEC.md §3.6, v1.14): the same four The Standard
/// sorts every exception into, so a stop reads the same way as a fault.
/// </summary>
public enum AgentFailureCategory
{
    Validation,
    DependencyValidation,
    Dependency,
    Service
}
