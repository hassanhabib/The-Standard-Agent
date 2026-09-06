// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Host.Security;

/// <summary>
/// The second lock on the front door. When the deployment configured authentication, an agent
/// route wants an authenticated user; the heartbeat stays open either way, and a deployment that
/// configured nothing is as open as before (principal review 2026-09-04, F-10).
/// </summary>
public static class IdentityGate
{
    public static bool Allows(bool authenticationConfigured, bool userIsAuthenticated, string path) =>
        authenticationConfigured is false
            || string.Equals(path, "/api/home", StringComparison.OrdinalIgnoreCase)
            || userIsAuthenticated;
}
