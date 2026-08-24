// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Host.Security;

/// <summary>
/// The host's front door. An agent endpoint carries approval and budget semantics, so the door
/// is not optional to think about — but it is optional to lock: no configured key means open,
/// which is what a laptop wants, and one configuration line locks every agent route for what a
/// deployment wants.
/// </summary>
public static class ApiKeyGate
{
    public static bool Allows(string? configuredKey, string? presentedKey, string path) =>
        throw new NotImplementedException();
}
