// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;

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
        string.IsNullOrEmpty(configuredKey)
            || string.Equals(path, "/api/home", StringComparison.OrdinalIgnoreCase)
            || KeysMatch(configuredKey, presentedKey);

    // Fixed-time on the bytes, because a comparison that returns at the first wrong character
    // hands an attacker the key one character at a time.
    private static bool KeysMatch(string configuredKey, string? presentedKey) =>
        presentedKey is not null
            && CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(configuredKey),
                Encoding.UTF8.GetBytes(presentedKey));
}
