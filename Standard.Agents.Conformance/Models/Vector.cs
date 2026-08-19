// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Conformance;

public sealed record Vector(
    string Name,
    string? Description,
    List<string> GeneratorReplies,
    Dictionary<string, string>? Tools,
    string Prompt,
    Expectation Expect,

    // Guardian inputs — all optional, so pre-guardian vectors deserialize unchanged.
    // Constitution / Consumption are inline markdown the harness writes to a file next to
    // the executable and points the real .Constitution()/.Consumption() builder at, so the
    // file-resolution path is exercised, not bypassed. GateVerdict / JudgeScore drive the
    // scripted guardians (default "allow" / "1.0"), letting a vector force a refusal.
    string? Constitution = null,
    string? Consumption = null,
    string? GateVerdict = null,
    string? JudgeScore = null,

    // Multi-run vectors. The decision log's durability (SPEC.md §4.7) and run isolation
    // (§4.4) are only observable across more than one run, so a vector may drive several
    // prompts through one agent instead of one. Prompts overrides Prompt when present;
    // Concurrent runs them all at once rather than in order, which is the only way to
    // certify that one run's bookkeeping never leaks into another's.
    List<string>? Prompts = null,
    bool Concurrent = false,

    // Turns on boundary redaction (SPEC.md 4.6) so a vector can certify that no model call
    // carries a sensitive value in the clear.
    bool Redact = false,

    // Perimeter controls (SPEC.md §4.9). RequireApproval names the acts an authority must
    // permit; ScreenToolOutput turns on screening of untrusted inbound; GateVerdictOnToolOutput
    // is the verdict the scripted Gate returns for tool output specifically, so a vector can
    // refuse a tool result while still accepting the prompt.
    List<string>? RequireApproval = null,
    bool ScreenToolOutput = false,
    string? GateVerdictOnToolOutput = null,

    // Resilience and budget (SPEC.md §4.10). CancelBeforeStart cancels the run before the first
    // turn; TransientFailures makes the scripted Brain fail that many times with a dependency
    // failure before succeeding, so retry can be certified without a real network.
    bool CancelBeforeStart = false,
    int TransientFailures = 0,
    int Retries = 0,
    int? MaxTurns = null,

    // Budget, retrieval and degradation (SPEC.md §4.2, §4.10).
    //
    // Wall-clock was the only bound a vector could express, which meant the TOKEN and COST bounds
    // were certified by nothing. An implementation could report no usage at all on the text
    // protocol, enforce neither bound, and still pass every vector and claim the Enterprise
    // profile — which is exactly what this one did until 1.3.0.
    double? BudgetMaxWallClockSeconds = null,
    int? BudgetMaxTokens = null,
    decimal? BudgetMaxCostUsd = null,
    decimal BudgetCostPerThousandTokens = 0m,
    Dictionary<string, string>? Knowledge = null,
    string? FallbackReply = null,
    int FailuresBeforeOpen = 3,
    int KnowledgeMaxResults = 3,

    // Sessions (SPEC.md 4.11). When set, every prompt in the vector runs in this conversation.
    string? SessionId = null,

    // Compensation (SPEC.md §4.9). CompensateOnFailure turns the unwind on; CompensatingTools
    // names the tools that know how to reverse themselves. A tool left out keeps the interface
    // default and must be reported as an effect that stands.
    bool CompensateOnFailure = false,
    List<string>? CompensatingTools = null,

    // Cross-process resumption (SPEC.md §4.9, §4.11). NewInstancePerPrompt builds a brand-new
    // agent for each prompt, so nothing but the files on disk survives between them;
    // DurableEffectLedger puts run-once in a folder rather than in the instance's memory.
    // Together they are the only way to certify that an effect outcome survives a crash.
    bool NewInstancePerPrompt = false,
    bool DurableEffectLedger = false,

    // A scripted authority — "pending", "approved", "denied" — answered in order, so a vector can
    // hold an act on one run and permit it on the next. That is the shape of every real approval:
    // the answer arrives after the agent already stopped.
    List<string>? ApprovalDecisions = null,

    // Native tool calling (SPEC.md §6). When set, the Brain is a V1 generator and its replies are
    // structured choices rather than lines of text.
    List<NativeReply>? NativeReplies = null,

    // Identity (SPEC.md §4.9). Who the host says is acting. Set it and the policy broker must be
    // told; leave it and the effect must claim no identity rather than invent one.
    string? Principal = null,

    // Tools this principal may not act with, refused by policy on the identity alone. This is a
    // scripted policy engine, not the allow-list: the allow-list cannot express "not for them".
    List<string>? DeniedForPrincipal = null);
