// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Conformance;

// One scripted specialist in the vector's fleet (SPEC.md §4.8 v1.6): the name a handoff calls,
// the description that advertises it, and a scripted brain of its own — so a handoff and a
// transfer can be certified without a network. RuleGate arms the specialist's own deterministic
// gate, which is how a specialist that REFUSES is scripted: the refusal is real, produced by
// the same control a host would configure.
[System.Text.Json.Serialization.JsonUnmappedMemberHandling(
    System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow)]
public sealed record FleetAgent(
    string Name,
    string Description,
    List<string> Replies,
    List<string>? RuleGate = null);
