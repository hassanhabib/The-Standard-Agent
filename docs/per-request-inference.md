# Per-Request Inference — The Missing Seam

How a single composed agent serves many callers, each asking for something different, without
rebuilding itself and without any caller widening the boundary the deployment set.

Short version: **the agent is configured once and asked many times, but today everything is
configured and nothing is asked.** This closes that gap at two edges, and leaves the six tiers
between them untouched.

First consumer: **LLooMA 2.0**, embedded as the backend of `PeerLLM.Experimental.Orchestrator` —
an agent whose brain is a network of peer LLMs and whose callers speak the OpenAI protocol. That
consumer forces three things this document now covers that earlier drafts did not: the seam must
reach the **streamed** path, the request must carry **caller-declared tools**, and the values that
flow below the entry must be **resolved by construction**, not by convention.

---

## 1 · The gap

`ProcessPromptAsync` takes a prompt, a session id, and a cancellation token. Nothing else. Every
inference parameter — temperature, max tokens, the shape the answer must take — is **instance
state**, fixed when the agent composes:

| Knob | Where it lives |
|---|---|
| temperature, max tokens, timeout | `InferenceSettings`, baked by `.Brain(...)` |
| response shape | `contractSchema` field, baked by `.Contract(...)` |
| turn budget | `maxTurns` field |

That is exactly right for an agent you *embed*: one deployment, one configuration, many prompts.
It is the wrong shape for an agent you *expose*, where each request carries its own parameters.

Neither workaround holds:

**Rebuild per request.** `Compose()` constructs `new GeneratorBroker(...)`, whose constructor does
`new HttpClient(...)`. An agent per request is a socket-exhausting HttpClient per request.

**Mutate a shared agent per request.** Worse. Every builder method routes through `Set()`, which
drops the cached composition. Request A calling `.Contract(schemaA)` recomposes the graph request B
is mid-run against. The `compositionLock` guards composition *integrity*; it does not provide
per-request *isolation*. That is a silent correctness bug, not a performance one.

And even if a value could get down there, the wire cannot express it. V0's `ChatCompletionRequest`
is a sealed five-field record. V1's `BuildRequest` writes `model`, `temperature`, `max_tokens`,
`messages`, `tools` — and stops.

**This document does not build an exposer.** Hosting remains backlog item #5, a separate package.
It builds the thing hosting is currently blocked on.

---

## 2 · The insight — it is two edges, not six tiers

`AgentContext` already flows the entire loop. It is the per-run carrier, already holding `Prompt`,
`SystemPrompt`, `History`, `ToolExchanges`, `PromptTokens`, `PendingEffect`. Per-request inference
options are the same kind of thing and belong on the same record.

On the V1 path the context **already reaches the Brain foundation**:

```csharp
ValueTask<GenerationResult> GenerateAsync(AgentContext context, IReadOnlyList<ToolDefinition> tools)
```

It arrives intact and is then discarded — `BrainService.V1.GenerateAsync` flattens it into
`messages` and calls the broker with `(messages, tools)`.

So the change is two edges:

| Edge | What changes |
|---|---|
| **Entry** — `StandardAgent` | A new overload takes a request, resolves precedence against configuration, and seeds `AgentContext` |
| **Last hop** — `BrainService` → broker | Stops discarding what it was already handed |

Everything between — Data, Decision, Direction, the coordinations, the orchestrations — is
untouched. No tier below the entry ever learns that precedence exists, because the context it
receives already carries **resolved** values.

That is the load-bearing rule of this design: **resolve once, at the boundary.** A loop that can
re-resolve is a loop where two turns of one run can disagree.

---

## 3 · The model — two records, because raw and resolved must not share a type

`PromptRequest` is what the caller says. It exists at the entry overload's signature and **never
travels below it**. What flows the loop is `ResolvedInference` — the output of precedence, a
different type, with no field for anything precedence discarded.

The split is not cosmetic. §4's central claim is that a dangerous state — model constrained to
schema A, guardian validating schema B — is *unreachable by construction*. That claim only holds
if no tier below the entry can ever see an unresolved caller value. If one type served both roles,
a broker handed "the request" could put the caller's discarded schema on the wire while the
guardian validates the configured one, and the guarantee would rest on discipline. Discipline is
a rule that could be forgotten; a missing field is not.

Both are records, not interfaces. Interfaces in this framework mark things that **do** something:
`ITool.ExecuteAsync`, `IGeneratorBroker.GenerateAsync`, `IPolicyBroker.AuthorizeAsync`. A request
does not *do* anything; it *is* something. Making it an interface the host implements would mean
the core cannot see inside it — every broker would downcast, reflect, or the interface would grow
a property per provider until it should have been a record anyway.

The schema travels as a **JSON string**, following `ToolDefinition.ParametersJson`, which already
set that precedent for the same reason: the core does not model schemas it did not write.

```csharp
public sealed record PromptRequest
{
    public string Prompt { get; init; } = "";

    // Session identity is data and rides the record. The CancellationToken does not — it is
    // runtime control, not a statement about the request — so the entry keeps it as a parameter:
    // ProcessPromptAsync(PromptRequest request, CancellationToken cancellationToken = default).
    public string SessionId { get; init; } = "";

    // The shape the answer must take. Null means the caller expressed no opinion.
    public string? ResponseSchemaJson { get; init; }

    // Nullable throughout, because "unset" must be representable — precedence depends on
    // distinguishing a value the caller chose from one they never mentioned.
    public double? Temperature { get; init; }
    public int? MaxTokens { get; init; }
    public int? Seed { get; init; }
    public IReadOnlyList<string> Stop { get; init; } = [];

    // Tools the CALLER will execute, declared so the model may name them (§6). The agent never
    // runs one — there is no code path from this list to Direction's registry. OpenAI-standard
    // callers arrive with these; ToolDefinition is already their wire shape.
    public IReadOnlyList<ToolDefinition> CallerTools { get; init; } = [];

    // What the core cannot model and should not try to: chat_template_kwargs is vLLM's,
    // thinking is Anthropic's, grammar (GBNF) is llama.cpp's. Carried opaquely, never read by
    // the core, handed to the broker whole — under the merge rule in §4.4.
    public string? ProviderOptionsJson { get; init; }
}

public sealed record ResolvedInference
{
    // Concrete, not nullable: precedence has already run, so the third rung (the framework
    // default) has already been applied. No tier below the entry ever supplies a default.
    public double Temperature { get; init; }
    public int MaxTokens { get; init; }

    // Nullable where "absent from the wire" is itself the meaning.
    public int? Seed { get; init; }
    public IReadOnlyList<string> Stop { get; init; } = [];

    // The schema that SURVIVED precedence — configured when a Contract exists, the request's
    // otherwise. There is no field for the losing schema, which is what makes §4.1's dangerous
    // state unreachable rather than avoided.
    public string? ResponseSchemaJson { get; init; }

    public IReadOnlyList<ToolDefinition> CallerTools { get; init; } = [];
    public string? ProviderOptionsJson { get; init; }
}
```

`AgentContext` gains one member: `public ResolvedInference Inference { get; init; }` — seeded on
**every** run, including the plain-string overloads, where it resolves configured → framework
default with no request rung. Always-seeded means no tier ever branches on "was there a request,"
and the legacy path and the request path are one path.

**What is deliberately absent.** Executable tools, permissions, budget, redaction, approvals,
principal. Not "ignored when configured" — **absent**. A request has no field in which to ask for
them, which is a stronger guarantee than a rule that could be forgotten. `CallerTools` is not the
exception it appears to be: it grants the model *vocabulary*, never the agent *capability* — see
§6 for why that distinction is load-bearing and where it is enforced.

---

## 4 · Precedence — configured always wins

> **What is established and hard-configured takes precedence, always.**

Resolution order, per field: **configured → request → framework default.** Applied once, at the
entry, by the only component that can see both the configuration and the request. Every value
below the entry is the ladder's output; brokers decide nothing.

This is not a tiebreak, it is the perimeter restated. A caller can never widen the boundary the
deployment set — cannot raise a budget, add a capability, disable redaction, or loosen a schema by
asking nicely. Configuration is a **ceiling**, not a suggestion.

`Seed` and `Stop` have no counterpart in `InferenceSettings`, so for them the first rung cannot
exist and precedence is trivially request → default (absent). Stated so no implementer wonders
what "configured wins" means for a field that cannot be configured.

### 4.1 The Contract collision, resolved

| Case | Wire (`response_format`) | Guardian validation |
|---|---|---|
| Contract configured | the **configured** schema | the **configured** schema |
| No Contract configured | the **request's** schema | the **request's** schema |

The request's schema is never merged, never partially honored, and never validated against a
different schema than the one that was sent. That last combination is the only genuinely dangerous
one — a model constrained to schema A and validated against schema B loops on `AgentStatus.Revising`
until `MaxTurns` burns out, with a trace showing repeated failures against a schema the model was
never given. Under this design that state is unreachable by construction — `ResolvedInference`
carries one schema field, the survivor, and the wire and the guardian both read it (§3).

The second row matters as much as the first. A request schema seeds **both** the wire and the
guardian, because plenty of engines accept `response_format` and quietly ignore it — local ones
especially. A schema that only reached the wire would be weaker than a configured one for no reason.

### 4.2 The blocker: "configured" is not currently representable

The rule requires knowing whether a value *was* configured.

For Contract this already works — `contractSchema` is `string?`, null when unset.

For inference parameters it does not. `.Brain()` takes optional parameters with defaults:

```csharp
public StandardAgent Brain(string apiUrl, string apiKey, string model,
    double temperature = 0.7, int maxTokens = 1024, int timeoutSeconds = 120)
```

and `InferenceSettings` holds them as non-nullable `double` / `int`. The moment anyone calls
`.Brain()` — which they must — temperature *is* `0.7`, and nothing distinguishes "the host chose
0.7" from "the host said nothing about temperature."

Applied literally against that, "configured wins" would mean **per-request temperature and max
tokens can never take effect**, always beaten by a default nobody expressed an opinion about.

**Required change:** `InferenceSettings.Temperature` and `MaxTokens` become `double?` / `int?`,
and the framework default becomes the **third rung of the ladder, applied in the same entry
resolution as the other two.** Not at the broker — a default applied at the broker would split
resolution across two places, in defiance of §2, and would put a decision inside a component The
Standard holds to be a thin liaison that decides nothing. Brokers receive concrete values and
write them. "Hard configured wins" then means what it says, instead of "defaulted wins."

### 4.3 Say so in the trace

When a request's schema is discarded because a Contract is configured, log it.

The framework already narrates its guardian decisions — `"Contract → REJECTED: {reason}"` — on the
stated principle that *a rejection the trace does not explain is a turn nobody can account for.* A
caller who sent a `response_format` and got a differently-shaped answer deserves the same courtesy.
One line, no behavior change.

### 4.4 The merge rule that keeps `ProviderOptionsJson` from being a hole

The bound on the opaque bag is **inference-shaping only** — and that bound is only real if the
merge enforces it. The broker is exactly where every request becomes wire bytes, and the wire
carries `tools` and `messages`; a naive merge of a passthrough containing a `tools` key would add
a tool at the wire level, which is precisely what §3 promises cannot happen.

So the rule, stated as spec rather than hoped as intent: **every core-owned key is
non-overridable.** `model`, `messages`, `tools`, `response_format`, `temperature`, `max_tokens`,
`seed`, `stop` — a colliding key in `ProviderOptionsJson` is stripped and the collision logged.
The modeled field wins because the modeled field is the one precedence was applied to; a raw key
that could beat it would be a second resolution path with no ceiling. What survives the strip —
`chat_template_kwargs`, `thinking`, `grammar`, whatever the engine understands — merges whole.

Under that rule the §3 claim holds: the bag cannot add a tool, raise a budget, or alter a
permission, because the keys through which it would do so are exactly the ones it cannot touch.

---

## 5 · The broker contract — additive, nobody moves

`docs/generator-contracts.md` makes a promise worth keeping: *"V0 is not deprecated and is not going
away… the five provider packages implement it and none of them should have to move on our
schedule."* Adding a parameter to `GenerateAsync` breaks all five on our schedule.

The pattern that avoids this is already in the codebase twice. `ITool` adds `Description`,
`Parameters`, `Risk`, `ScopeOf`, and `CompensateAsync` as **default interface members** — existing
tools compile untouched. `IBrainService` uses the same trick to bolt on the entire V1 path
(`SpeaksNatively => false`, plus a `GenerateAsync` overload that throws by default).

Same move here — and note the overloads take `ResolvedInference`, never `PromptRequest`. A broker
is below the entry; §3 says what it sees is the ladder's output:

```csharp
public interface IGeneratorBroker
{
    ValueTask<string> GenerateAsync(string systemPrompt, string userPrompt);

    IAsyncEnumerable<string> GenerateStreamAsync(
        string systemPrompt, string userPrompt,
        CancellationToken cancellationToken = default);

    /// <summary>True when this broker puts resolved inference options on the wire.</summary>
    bool HonorsRequest => false;

    ValueTask<string> GenerateAsync(
        string systemPrompt, string userPrompt, ResolvedInference inference) =>
        GenerateAsync(systemPrompt, userPrompt);

    IAsyncEnumerable<string> GenerateStreamAsync(
        string systemPrompt, string userPrompt, ResolvedInference inference,
        CancellationToken cancellationToken = default) =>
        GenerateStreamAsync(systemPrompt, userPrompt, cancellationToken);
}
```

`IGeneratorBrokerV1` gains the mirror overloads. Every provider package keeps compiling; each opts
in when it chooses.

**The stream is not an afterthought.** The streamed loop is a run like any other — the codebase
already says so at `ProcessPromptStreamAsync` — and it is the main path for the first consumer:
an orchestrator's peers will stream. A seam that reached only the batched call would leave a
streamed request silently ignoring its temperature with no trace to say so. The mirror overloads
above are why that cannot happen; the streamed decision path passes `context.Inference` exactly as
the batched one does.

**Graceful degradation is a property, not an accident.** A broker that has not opted in silently
ignores the resolved options — and the answer is *still* held to shape, because the Contract
guardian validates and revises regardless, on both the batched and streamed paths. Constrained
decoding becomes an optimization over a guarantee the architecture already provides.
`HonorsRequest` exists so the trace can say which one happened: *"shape enforced by guardian only;
broker does not honor requests."*

### 5.1 The built-ins

`GeneratorBrokerV1` is easy — it already builds a `JsonObject`, for the stated reason that *"the
tool schemas are already JSON the host wrote and re-modelling them would only give us a second place
for them to be wrong."* `response_format` and the §4.4-merged `ProviderOptionsJson` land naturally.

`GeneratorBroker` (V0) is the real cost item: its `ChatCompletionRequest` is a sealed five-field
record. **Ruling:** converge it onto the same `JsonObject` construction, for the reason V1
already documents. Growing five optional record fields solves this request and re-opens the same
question at the next one.

---

## 6 · Caller tools — vocabulary, never capability

The OpenAI protocol carries `tools` in the request body, and the first consumer's callers arrive
speaking it. §3 lists tools among the deliberately absent — so this section exists to say, as
spec, why `CallerTools` is not that, and where the difference is enforced.

In the OpenAI protocol the server **never executes** a caller's tool. The model names one, the
server returns the `tool_call`, and the *caller* executes it and posts the result back on the next
request. Caller tools are therefore not perimeter tools. They are **vocabulary handed to the
model** — words it may answer with — and a call naming one is not an act for Direction to perform.
It is a **terminal answer addressed to the caller.**

What §3 excludes is *capability*: a field through which a request could make the agent execute
something the deployment did not configure. `CallerTools` grants none. There is no path from that
list into Direction's registry, no `ITool` behind any entry, and nothing for the perimeter —
policy, approval, run-once — to even evaluate, because the agent never acts on one.

### 6.1 On the wire

Caller tools are appended to the configured tool list handed to the Brain — the V1 `tools`
parameter, or the text protocol's tool listing on V0. The model sees one vocabulary; only
Direction knows which words are whose.

**Name collision is resolved by the perimeter rule.** A caller tool whose name matches a
configured tool is dropped and the collision logged. Configured wins, as everywhere in §4 —
and unambiguously, so a `tool_call` naming that tool has exactly one meaning: the configured
tool, executed locally, under every configured control.

### 6.2 A foreign call is a pending effect

Direction classifies a returned `tool_call` by name:

| Name is | Meaning | What happens |
|---|---|---|
| A configured tool | An act | Executed under the perimeter, as today |
| A caller tool | A terminal answer | Run ends `AwaitingInput`; the call rides out as the pending effect |
| Neither | A hallucination | The existing unknown-tool path, unchanged |

The framework already models "the run pauses; something outside this process must act and report
back" — that is `AgentStatus.AwaitingInput` and `AgentContext.PendingEffect`, built for human
approval and structurally identical here. A foreign call becomes
`AgentEffect.For(runId, toolName, argumentsJson)` — the identity, idempotency key, and session
ride-out all come for free — and the run ends with it pending. The caller executes, posts the
result on the same session, and the run resumes with the exchange recorded as a `ToolExchange`,
so the V1 conversation rebuild emits it as the tool message the downstream model expects. The
resume mirrors the approval resume; no new status and no new loop semantics were invented,
which is the argument that the mapping is right.

The authority over a pending effect was always "whoever the deployment says may answer." For an
approval it is a human; for a foreign call it is the caller. Same seam, different authority.

---

## 7 · What this unblocks

Because per-request values ride the context rather than the builder, `Set()` is never called per
request. The composition cache holds. One `HttpClient`. One composed agent serving concurrent,
heterogeneous requests — batched and streamed — which is precisely what an endpoint needs and what
is impossible today.

**Acceptance criterion:** N concurrent prompts with N different schemas and temperatures against one
`StandardAgent` instance — some batched, some streamed — with exactly one composition and one
`HttpClient` for the run.

Concretely, for the first consumer: LLooMA 2.0 becomes a `StandardAgent` whose exposer translates
OpenAI ⇄ `PromptRequest`, whose brain is the peer-routing custom mode (the roster and the fan-out
live *inside* the brain, below the agent's awareness — routing is dispatch, not Knowledge), whose
web access is Tavily as a configured external tool, and whose caller-tool exchanges ride sessions.
All composition, no framework surgery — which is the test this seam was designed to pass.

---

## 8 · Deliberately not in scope

- **An HTTP exposer.** Backlog #5, a package. This is its prerequisite, not its replacement.
- **The native streamed path.** `DecideStreamAsync` does not branch on `SpeaksNatively`; streaming
  rides the text protocol today. An orchestrator whose peers stream native `tool_call` deltas will
  need a streamed V1 contract — that is real, it is next, and it is its own document, because it
  changes how a choice is *read* mid-stream and this one only changes what rides alongside it.
- **Grammar / GBNF as a first-class field.** Decode-time and engine-specific; it rides
  `ProviderOptionsJson` for any engine that wants it, under the §4.4 merge rule.
- **Per-request executable tools, permissions, budget, approvals.** Perimeter. Configuration only,
  by §4. (`CallerTools` is not this — §6.)
- **Parallel tool calls.** One act per turn remains a perimeter decision, not an oversight.
- **A typed `T` overload with schema generation.** Needs reflection; belongs in a package, not a
  dependency-free core.

---

## 9 · Sequence

Each step ships on its own and leaves the build green.

| # | Step | Why here |
|---|---|---|
| 1 | `PromptRequest` + `ResolvedInference` + `AgentContext.Inference` | The vocabulary, both records. Nothing reads them yet. |
| 2 | `InferenceSettings` nullable; entry resolution applies all three rungs | §4.2 — precedence is unimplementable until this lands; brokers stay decisionless |
| 3 | `ProcessPromptAsync(PromptRequest, CancellationToken)` + the streaming twin | Resolve once, at the boundary, on both paths |
| 4 | Default-member overloads — batched and stream — on `IGeneratorBroker` / `IGeneratorBrokerV1` | Additive; five packages untouched |
| 5 | `BrainService` stops discarding; `GeneratorBrokerV1` honors, §4.4 merge enforced | First end-to-end path |
| 6 | `GeneratorBroker` (V0) converges onto `JsonObject` and honors | The larger of the two |
| 7 | Caller tools: wire merge, collision drop, Direction classification, pending-effect return | §6; depends on the vocabulary from step 1 only |
| 8 | Trace lines: override announced, collision logged, `HonorsRequest` reported | §4.3, §4.4 |

---

## 10 · Definition of done

The bar this repository already holds, restated so nothing slips:

- **FAIL/PASS TDD**, and the FAIL commit is a test *run and observed failing*. Where a test is
  written after the code, it is sabotage-verified and the commit says so.
- **Brokers carry no unit tests.** They are thin liaisons.
- **The triad is complete** — Local, External, Custom — or the capability matrix test fails.
- **The tier rules hold.** Note this design adds no foundation and no broker: it widens two
  existing contracts, one existing model, and one existing status's set of causes.
- **Conformance vectors, proven able to fail:**
  - `configured-contract-overrides-request-schema`
  - `request-schema-applies-when-unconfigured`
  - `request-schema-seeds-guardian-not-only-wire`
  - `broker-without-request-support-degrades-to-guardian`
  - `concurrent-heterogeneous-requests-share-one-composition`
  - `streamed-request-honors-resolved-inference`
  - `provider-options-cannot-touch-core-owned-keys`
  - `caller-tool-never-executes`
  - `caller-tool-call-ends-run-as-pending-effect`
  - `caller-tool-name-collision-drops-caller-tool`
- **The spec says it** where an implementer would otherwise guess — particularly §4 precedence and
  §6 caller-tool semantics, which another language must reproduce exactly.
- Zero warnings, all four profiles certified.

---

## 11 · Rulings

Formerly open questions, now closed. Recorded because each shaped the answer above.

1. **`PromptRequest` subsumes `sessionId`; the `CancellationToken` stays a parameter.** Session
   identity is data about the request; a token is runtime control and does not belong on a data
   record. One record overload, not four string ones.

2. **On key collision, the modeled field wins — generalized.** Not just `temperature` against a
   raw `temperature`: every core-owned key is non-overridable through `ProviderOptionsJson`,
   stripped and logged (§4.4). The narrower ruling would have left `tools` and `messages` open,
   which is the hole the perimeter exists to close.

3. **Ignore, not clamp.** Configuration wins outright; a request value against a configured field
   is discarded and logged. The clamp reading — configuration as a *bound* a request may move
   inside of — needs no core machinery to exist: the host composed the agent, knows every
   configured ceiling, and can clamp an incoming request before calling `ProcessPromptAsync`.
   Bound semantics is hosting-package policy, fully expressible today. The road not taken costs
   nothing to not take.

4. **Quiet degradation in the core; loudness is hosting policy.** A broker that cannot honor an
   option ignores it, the guardian still enforces shape, and `HonorsRequest` puts the fact in the
   trace. Whether that deserves a 400 is the exposer's call, and it has what it needs to make it.
   `HonorsRequest` is deliberately coarse — a bool, not a capability matrix; a broker that honors
   temperature but not seed reports true and the trace carries the nuance. Chosen, not overlooked.

5. **V0 converges onto `JsonObject`.** For the reason V1 already documents — the alternative
   answers this request and re-asks the question at the next one.

6. **Caller tools are core spec, not a hosting-side pattern.** The semantics — vocabulary not
   capability, collision drop, foreign call as pending effect — are protocol truths any exposer in
   any language will need, and §10 requires the spec to speak where an implementer would guess.
   Hence §6.
