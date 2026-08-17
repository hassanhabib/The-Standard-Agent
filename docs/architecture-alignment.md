# Architecture Alignment — Honouring The Standard

> The enterprise program (0.19 → 1.1) added ten capabilities and, in doing so, put five brokers
> where The Standard does not allow brokers to be. This document records the deviation, the target
> shape, and the order of the work to get there.

Nothing here changes behaviour. The 30 conformance vectors and 387 unit tests are the safety net
for every step, and none of them should so much as flicker.

---

## 1 · The deviation

Four violations, ranked worst first. All were introduced by the enterprise program; none existed in
the original 1·3·9.

| # | Violation | Where |
|---|---|---|
| **A** | **A broker depends on two other brokers** | `LoggingBroker` holds `ITimeBroker` and `IAuditBroker` |
| **B** | An orchestration holds brokers | `DirectionOrchestrationService` → Policy, Approval, EffectLedger |
| **C** | Coordination holds brokers | `AgentCoordinationService` → Session (a real resource), Time (pure) |
| **D** | A foundation holds extra brokers | Gate, Judge → Redaction; Brain → Redaction, Resilience |

**A** is the worst and was the last to be noticed. It sits at the bottom of the stack and breaks
Invariant 3 — *brokers are thin*.

### Why this is not cosmetic

A foundation service gives three things: **validation**, **exception mapping**, and **attribution**.
A tier that calls a broker directly gets none of them.

When `FileEffectLedgerBroker` hits a full disk there is no `EffectLedgerDependencyException`. The
`IOException` surfaces unmapped and is attributed to *Direction* — so the trace blames the
orchestration for a storage failure. Every other resource in the framework is wrapped. These five
were not.

---

## 2 · The target

![Target architecture](https://raw.githubusercontent.com/hassanhabib/The-Standard-Agent/main/assets/the-standard-agent-architecture-target.png)

Source: [`assets/the-standard-agent-architecture-target.svg`](../assets/the-standard-agent-architecture-target.svg)

```
CLIENT          StandardAgent
MANAGEMENT      RunManagementService                    the loop, one run
COORDINATION    Data · Decision · Direction             the three natures
ORCHESTRATION   6 regions                               2–3 foundations each
FOUNDATION      14 services                             one broker each
BROKER          13 nature brokers + 4 utility
```

### The six regions

| Nature | Orchestration service | Foundations |
|---|---|---|
| Data | `RetrievalOrchestrationService` | Skill, Knowledge |
| Data | `RecollectionOrchestrationService` | Memory, Session |
| Decision | `InferenceOrchestrationService` | Brain, Redaction |
| Decision | `GuardianOrchestrationService` | Gate, Judge, Redaction |
| Direction | `PerimeterOrchestrationService` | Policy, Approval, EffectLedger |
| Direction | `ExecutionOrchestrationService` | InternalTool, ExternalTool, Return |

The regions come from **conceptual normalization**, not from counting. Direction was never one
concept with six parts; it was two concepts sharing a box — *may this act happen* and *do it*. Split
on the concept and both land on three.

Data re-cut the same way, and the result is less obvious than it looks: **Skill and Knowledge**
belong together because both are authored and both are selected *by relevance*. **Memory and
Session** belong together because both are accumulated and both are replayed.

### The number

Every one of the five new foundations is an OPTIONAL capability in the spec — §4.6 redaction, §4.9
policy/approval/ledger, §4.11 sessions. The nine originals are exactly the Core set.

> **Core is 1 · 3 · 9. Full is 1 · 3 · 6 · 14.**

Drop the five and each nature holds exactly three, the regions collapse, and the natures are plain
orchestrations again. 1·3·9 was already perfect 2–3 compliance — which is why it felt right.

---

## 3 · Decisions, and the reasoning that survived scrutiny

**`RunManagementService`, not `LoopManagementService` or `AgentManagementService`.**
`{Entity}{Tier}Service` wants the thing being managed. A loop is a control structure — one step
further and you get `WhileManagementService`. The entity is already named in the spec and the code:
`AgentRun`, `runId` on every audit record, §4.4's *"each invocation MUST establish its own run
identity."* It is also unambiguous, where "loop" is not: there are two loops in there, the turn loop
and the Judge revision loop.

**Audit stays a utility broker; it does not become a foundation.** Every tier emits decision
records, foundations included. Make it a foundation and every log line becomes a foundation calling
a sibling. Logging has exactly this shape and The Standard's answer is that it is a *utility* broker
any tier may hold. Audit is the same category — an observability sink, not a business capability.
The real fix here is violation **A**: pull the audit broker *out* of the logging broker.

**Resilience is a broker, and it is applied by decoration.** It has no resource of its own; its
whole job is to wrap another broker's call. A foundation whose purpose is to wrap another
foundation's call would be the violation, not the fix.

```csharp
new RetryingGeneratorBroker(new GeneratorBroker(...))
```

`BrainService` then holds exactly one broker and does not know retry exists. Note this is *stricter*
than "a utility broker held by any tier": logging earns that exemption because it is write-only
observability, while resilience changes control flow — it decides whether the call happens again.

**File stays a broker.** It is already the Local resource *behind* `MemoryService` and
`KnowledgeService`. Promoting it would put a foundation under a foundation.

**Redaction is a foundation, shared by both Decision regions.** §4.6 requires every model call
redacted, and Gate and Judge are model calls — so `GuardianOrchestrationService` depends on it as
well as `InferenceOrchestrationService`. Two orchestrations sharing a foundation is legal and
normal; what matters is that the concept lives in one place.

*Recorded dissent:* a decorating broker would make §4.6 true **by construction** rather than by
being remembered in two places. The foundation form was chosen deliberately; if the invariant ever
regresses, this is where to look first.

---

## 4 · The order of work

Each step is its own branch with FAIL/PASS commits. Behaviour is unchanged throughout, so the
vectors gate every one.

| # | Step | Fixes |
|---|---|---|
| 1 | `SessionService` foundation, Coordination stops holding the broker | C |
| 2 | `PolicyService` foundation | B |
| 3 | `ApprovalService` foundation | B |
| 4 | `EffectLedgerService` foundation | B |
| 5 | `RedactionService` foundation; redact/rehydrate moves out of Brain, Gate, Judge | D |
| 6 | Resilience becomes a decorating broker; `BrainService` drops it | D |
| 7 | Un-nest audit and time from `LoggingBroker` | A |
| 8 | Six orchestration services, one per region | 2–3 rule |
| 9 | Three coordination services, one per nature | 2–3 rule |
| 10 | `AgentCoordinationService` → `RunManagementService` | naming |
| 11 | SPEC.md §4.2/§4.3 state the placement rule; README and diagram swap | the record |

Steps 1–7 are independent and can land in any order. 8–10 are sequential. Step 11 lands last,
because the README should describe what ships rather than what is planned.

**No public API change.** `docs/support.md` already states that service classes and their
constructors are not a public contract, and the builder surface is untouched. This is `1.2.0.0` —
a service change, segment 2.

---

## 5 · What this does not fix

`Return Service` wraps no broker, so it is not a foundation by the strict reading — it is a pure
function that Direction happens to call through a service seam. It stays as it is: renaming it or
folding it into `ExecutionOrchestrationService` would cost more comprehension than the purity buys,
and it has been part of the published nine since the beginning.
