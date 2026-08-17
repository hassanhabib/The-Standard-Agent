// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Conformance;

/// <summary>
/// A readiness profile: the named set of vectors an implementation must pass to claim it.
/// Profiles turn "enterprise-ready" from a claim into something a build can fail.
/// </summary>
/// <param name="Name">Core, Reliable, Enterprise or Critical.</param>
/// <param name="Description">What an agent at this profile can be trusted to do.</param>
/// <param name="Inherits">The profile this one builds on; its requirements apply too.</param>
/// <param name="Requires">
/// Vector names that must pass. A named vector that does not exist is a failure, not a skip —
/// otherwise a profile could be claimed by simply never writing its evidence.
/// </param>
public sealed record Profile(
    string Name,
    string? Description,
    string? Inherits,
    List<string> Requires);
