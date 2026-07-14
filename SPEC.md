# The Standard for Agents — Specification
**Version 0.1 (draft)**

A **normative, language-neutral** blueprint for building a Tri-Nature agent framework
in any language (JavaScript, .NET, Go, Rust, Python, …). The reference implementation
is `Standard.Agents` (C#). The rationale and philosophy live in the companion theory,
*The Tri-Nature of Agent* (`THE-TRI-NATURE-OF-AGENT.md`); this document is the buildable
contract.

---

## 1. Conformance

The keywords **MUST**, **MUST NOT**, **SHOULD**, **SHOULD NOT**, **MAY** are to be
interpreted as in RFC 2119.

An implementation is **Standard-Agents Conformant** at a given **profile** (§8) if it
satisfies every MUST for that profile. Conformance is about **contracts and behavior**,
not file layout or language idiom.

---

## 2. Notation

Neutral pseudo-types — map each to your language:

| Spec | Meaning |
|---|---|
| `Text` | UTF-8 string |
| `Number` | integer or real |
| `Bool` | boolean |
| `List<T>` | ordered, immutable sequence |
| `Map<K,V>` | key → value |
| `Async<T>` | a value delivered asynchronously (Promise / Future / Task / coroutine) |
| `Void` | no value |
| `enum { … }` | a closed set of named values |

`interface Name { method(params) -> Return }` denotes a **contract** (a role), not a
class. The names are canonical *roles*; adapt casing to your language.

---

## 3. Data Types (MUST)

### 3.1 AgentStatus

```
enum AgentStatus { Working, Responded, AwaitingInput, Refused, Failed }
```

- `Working` **MUST** be the default / initial value.
- The loop (§5) continues while `status == Working` and stops on any other value.
- Implementations **MUST NOT** replace this with a boolean.

### 3.2 AgentContext

The single carrier that threads the loop. **All content is Data**; the regions mark
which nature last wrote each field.

```
record AgentContext {
  prompt        : Text          // the task (input)
  systemPrompt  : Text          // DATA:      written by Recall
  observations  : List<Text>    // DATA:      written by Recall / Act
  intent        : Text          // DECISION:  written by Think
  directionType : Text          // DECISION:  written by Think
  payload       : Text          // DECISION:  written by Think
  rawReply      : Text          // DECISION:  written by Think
  result        : Text          // DIRECTION: written by Act
  status        : AgentStatus   // DIRECTION: written by Act
}
```

- Context updates **MUST** be copy-on-write: a nature returns an updated copy; a nature
  **MUST NOT** mutate a shared instance. (Use records/immutable structs; if unavailable,
  a copy helper or builder.)

---

## 4. Component Contracts

The architecture is four tiers: **Coordination → Orchestration → Foundation → Broker**.
Flow is forward only; a tier **MUST NOT** call a tier above it.

### 4.1 Brokers — thin liaisons

A broker is a liaison to **exactly one** external resource. A broker:
- **MUST** integrate one resource only.
- **MUST NOT** contain business flow control (no branching/looping for business decisions).
- **MUST NOT** author prompts, rules, or rubrics — those are Data.
- **MUST** expose asynchronous methods.

```
interface SkillBroker      { selectSkills() -> Async<Text> }
interface MemoryBroker     { selectMemories() -> Async<List<Text>>;  insertMemory(m: Text) -> Async<Void> }
interface KnowledgeBroker  { selectKnowledge(query: Text) -> Async<List<Text>> }
interface ClassifierBroker { classify(systemPrompt: Text, input: Text) -> Async<Text> }
interface GeneratorBroker  { generate(systemPrompt: Text, userPrompt: Text) -> Async<Text> }
interface VerifierBroker   { verify(systemPrompt: Text, candidate: Text) -> Async<Number> }   // 0.0–1.0
interface ToolBroker       { has(name: Text) -> Bool;  run(name: Text, input: Text) -> Async<Text> }
interface McpBroker        { call(name: Text, input: Text) -> Async<Text> }
interface LogBroker        { reset() -> Async<Void>;  write(line: Text) -> Async<Void> }   // support broker
```

### 4.2 Foundation Services — one broker each

A foundation wraps one broker, returns **primitives**, and speaks business language.

```
interface SkillService        { retrieveSkills() -> Async<Text> }
interface MemoryService       { recallMemories() -> Async<List<Text>>;  remember(m: Text) -> Async<Void> }
interface KnowledgeService    { retrieveKnowledge(query: Text) -> Async<List<Text>> }
interface GateService         { screen(gatePrompt: Text, input: Text) -> Async<Text> }
interface BrainService        { generate(systemPrompt: Text, userPrompt: Text) -> Async<Text> }
interface JudgeService        { evaluate(judgePrompt: Text, candidate: Text) -> Async<Number> }
interface InternalToolService { handles(name: Text) -> Bool;  run(name: Text, input: Text) -> Async<Text> }
interface ExternalToolService { call(name: Text, input: Text) -> Async<Text> }
interface ReturnService       { return(payload: Text) -> Async<Text> }   // NO broker — the dead end
```

### 4.3 Orchestration Services — one per nature

Each coordinates its nature's foundations and returns an **updated AgentContext**.

```
interface DataOrchestration      { recall(ctx: AgentContext) -> Async<AgentContext> }
interface DecisionOrchestration  { think(ctx: AgentContext)  -> Async<AgentContext> }
interface DirectionOrchestration { act(ctx: AgentContext)    -> Async<AgentContext> }
```

Required behavior:
- **recall** — **MUST** set `systemPrompt` from `SkillService`; **MAY** seed `observations`
  from Memory/Knowledge. It refreshes what the agent HAS.
- **think** — **MUST**: (a) Gate screens the input, (b) Brain generates, (c) interpret the
  reply into `intent`/`directionType`/`payload`/`rawReply`. It **MAY** run Judge on a final
  answer and loop instead of returning (reflective judgment). It **MUST NOT** author prompt
  text.
- **act** — **MUST** route by `directionType`: **Return** (terminal), **Internal** (a local
  tool), or **External** (out across the boundary). A non-terminal result **MUST** be
  appended to `observations`, and its `status` **MUST** be `Working`.

### 4.4 Coordination — the Agent

```
interface AgentCoordination { processPrompt(prompt: Text) -> Async<Text> }
```

It runs the loop (§5) and **MUST** hold no nature logic beyond sequencing and observing
(logging). An `AgentCoordination` **SHOULD** also satisfy the tool contract (§6) so an
agent can be nested as a tool of another agent (the fractal).

---

## 5. The Loop (normative algorithm)

```
processPrompt(prompt):
    logBroker.reset()
    context := AgentContext { prompt: prompt }        // status defaults to Working
    for turn in 1 .. MAX_TURNS:                        // MAX_TURNS MUST be finite; SHOULD be small
        context := dataOrchestration.recall(context)
        context := decisionOrchestration.think(context)
        context := directionOrchestration.act(context)
        if context.status != Working: break
    return context.result
```

- The loop **MUST** cap iterations (`MAX_TURNS`).
- Direction **MUST** feed non-terminal results back into `observations`, and the next
  Decision **MUST** be able to read them (this is how tool results and fetched data return
  to the Brain).

---

## 6. The Tool / Reply Protocol

The Brain expresses its choice as text (**reference protocol**, SHOULD support) or via a
provider-native tool-call mechanism (**MAY**).

Reference protocol:
- To act: a single line — `ACTION: <toolName>: <input>`
- To answer: `FINAL: <answer>`

Parsing (MUST):
- A tool call **MUST** be read from the **first line only** (models often emit extra lines).
- A final answer **MAY** span multiple lines.
- `directionType == "ReturnResponse"` is terminal → `Responded`.
- `directionType == "Refuse"` is terminal → `Refused`.
- Any other `directionType` is a tool name, routed by Direction (§4.3).

---

## 7. Invariants (MUST / MUST NOT)

1. **Everything is Data.** Produced content (a reply, a tool result, a fetched document)
   **MUST** be treated as Data once produced.
2. **Prompts, rules, and rubrics live in Data.** They **MUST** be loaded from skills/config
   and **MUST NOT** be authored in Decision, in brokers, or hardcoded.
3. **Brokers are thin.** A broker **MUST NOT** contain business flow control.
4. **The agent is ephemeral.** An instance **MUST** be stateless across prompts. Persistent
   memory **MUST** live outside the agent — recalled by Data, written by Direction.
5. **A draft is not a commitment.** Where a guardian applies, output **MUST NOT** cross a
   boundary un-vetted.
6. **No self-certification.** A guardian (Gate/Judge) **MUST NOT** be the Brain. A faculty
   **MUST NOT** certify its own trustworthiness.
7. **Irreversible before, not after.** An irreversible action **MUST** be authorized before
   execution by a trusted guardian (deterministic rule and/or human), never after.
8. **The boundary.** External state **MUST** enter only as Data (via a Direction that reached
   out) and effects **MUST** leave only via Direction. The three natures are the agent's
   interior.
9. **Configuration is not a nature.** It **MUST** be consumed by brokers to function, not
   reasoned over by Decision.

---

## 8. Conformance Profiles

### 8.1 Core — the minimal viable agent (MUST)

- `AgentContext`, `AgentStatus` (§3).
- The Coordination loop (§5).
- **Data:** `SkillService` + `SkillBroker`.
- **Decision:** `BrainService` + `GeneratorBroker`. Gate and Judge **MAY** be pass-through.
- **Direction:** `InternalToolService` + `ToolBroker`, and `ReturnService`.
- The reply protocol (§6).
- Invariants 1–4, 8, 9.

### 8.2 Full — adds

- `MemoryService`, `KnowledgeService` backed by real stores.
- `GateService` and `JudgeService` as **real guardians, distinct from the Brain**.
- `ExternalToolService` (MCP / remote).
- Invariants 5–7 (guardian & safety).

---

## 9. Porting Notes

- **Async:** use the language's native model; every contract method is asynchronous.
- **Immutability:** records/immutable structs with copy-update. If unavailable, a builder
  or an explicit `copyWith`.
- **Naming:** adapt case (PascalCase / camelCase / snake_case). The identifiers here are
  canonical roles, not literal names.
- **DI:** OPTIONAL. A hand-wired composition root is fully conformant.
- **Tiers:** the four tiers **SHOULD** be preserved as module boundaries. A small-scale
  implementation **MAY** collapse a tier that adds no value (e.g. back the three Decision
  brokers with one inference endpoint), provided the contracts and invariants still hold.
- **Inference:** `GeneratorBroker` **SHOULD** target an OpenAI-compatible
  `POST /v1/chat/completions` endpoint for interoperability, but **MAY** wrap any model.

---

## 10. Conformance Checklist

- [ ] `AgentContext` and `AgentStatus` as specified (§3)
- [ ] Loop caps turns; non-terminal results feed back into observations (§5)
- [ ] Brokers thin — one resource, no flow control, no authored prompts (§4.1, §7.3)
- [ ] Prompts / rules / rubrics loaded from Data, never hardcoded (§7.2)
- [ ] Agent instance stateless across prompts; persistent memory external (§7.4)
- [ ] Reply protocol: first-line ACTION, terminal ReturnResponse / Refuse (§6)
- [ ] **Full:** guardians (Gate/Judge) distinct from the Brain (§7.6)
- [ ] **Full:** irreversible actions authorized before execution (§7.7)

---

*Specification v0.1 — evolves with the theory. Reference implementation: `Standard.Agents` (C#).
Rationale: `THE-TRI-NATURE-OF-AGENT.md`.*
