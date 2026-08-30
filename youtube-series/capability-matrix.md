# The capability matrix — the series' non-negotiable

**Every capability is shown all three ways, on camera, in the episode that introduces it.** Not
named. Not tabled. Demonstrated, run, and left working.

This is the framework's central claim — nineteen capabilities, three verbs each, and a build that
fails if one is missing. A series that shows only the Local mode has taught the easy third and
quietly withheld the reason anyone would adopt this over an afternoon's worth of glue code.

**The verbs**

| Verb | Mode | Means |
|---|---|---|
| `.X(...)` | **Local** | In the box. Point at a file, a folder, a rule. No dependency. |
| `.UseX(broker)` | **External** | A provider package, or any broker you hand it. |
| `.OnX(delegate)` | **Custom** | Your own code, inline. |

---

## The nineteen, with exact signatures

| # | Capability | Local | External | Custom | Episode |
|---|---|---|---|---|---|
| 1 | Skills | `Skills(path)` | `UseSkills(broker)` | `OnSkills(Func<ValueTask<IReadOnlyList<Skill>>>)` | 2.1 |
| 2 | Memory | `Memory(path)` | `UseMemory(broker)` | `OnMemory(...)` | 2.4 |
| 3 | Knowledge | `Knowledge(path)` | `UseKnowledge(broker)` | `OnKnowledge(Func<string, ValueTask<IReadOnlyList<string>>>)` | 2.5 |
| 4 | Brain | — *documented N/A* | `UseGenerator(broker)` · `Brain(url,key,model)` | `OnBrain(delegate)` | 1.3 / 1.4 / 1.7 |
| 5 | Native brain | — *same reason* | `UseNativeBrain(broker)` · `NativeBrain(...)` | `OnNativeBrain(delegate)` | 5.3 |
| 6 | Gate | `RuleGate(...)` | `Gate(url,key,model)` · `UseGate(IClassifierBroker)` | `OnGate(delegate)` | 3.1 / 3.3 |
| 7 | Judge | `RuleJudge(...)` | `Judge(url,key,model)` · `UseJudge(IVerifierBroker)` | `OnJudge(delegate)` | 3.2 / 3.3 |
| 8 | Tools | `Tool(ITool)` · `Tools(...)` | `Mcp(endpointUrl)` · `UseMcp(IMcpBroker)` | `Tool(ITool)` — your own class | 2.2 / 2.3 |
| 9 | Trace | `LogTo(path, verbosity)` | `UseLogging(ILoggingBroker)` | `UseLogging(ILoggingBroker)` | 4.3 |
| 10 | Audit | `Audit(path)` | `UseAudit(IAuditBroker)` | `OnAudit(Func<AuditRecord, ValueTask>)` | 4.3 |
| 11 | Policy | `AllowTools(...)` | `UsePolicy(IPolicyBroker)` | `OnPolicy(delegate)` | 4.1 |
| 12 | Approval | `RequireApproval(...)` | `UseApprovals(IApprovalBroker)` | `OnApproval(delegate)` | 4.4 |
| 13 | Effect ledger | `EffectLedger(path)` | `UseEffectLedger(broker)` | `OnEffectLedger(...)` | 4.5 |
| 14 | Usage | `Usage(charactersPerToken)` | `UseUsage(IUsageBroker)` | `OnUsage(Func<string, ValueTask<int>>)` | 4.7 |
| 15 | Sessions | `Sessions(path, maxHistoryTurns)` | `UseSessions(ISessionBroker)` | `OnSessions(select, upsert)` | 4.8 |
| 16 | Resilience | `Resilience(retries)` | `UseResilience(IResilienceBroker)` | `Fallback(...)` | 5.1 |
| 17 | Redaction | `Redact(rules)` | `UseRedaction(IRedactionBroker)` | `OnRedaction(redact, rehydrate)` | 4.2 |
| 18 | Telemetry | `Telemetry(name)` | `UseTelemetry(ITelemetryBroker)` | `OnTelemetry((eventName, attrs) => …)` | 4.3 |
| 19 | Contract | `Contract(schema)` | `UseContract(broker)` | `OnContract(delegate)` | 7.3 |

**The two dashes are the only gaps in the whole framework**, and they are documented
impossibilities rather than debt: *Local* means "in the box, no dependency", and running a model
in-process needs an inference runtime. Say that out loud in 1.7 — a framework that names its two
gaps and explains them is making a different kind of promise than one that leaves you to find them.

**Beyond the triads — the loop capabilities (1.13–1.16), deliberately not rows:**

| Capability | Surface | Episode |
|---|---|---|
| Narration | `GenerationResult.Narration` · tool templates (`NarrationStarting`/`NarrationObserved`) | 7.9 |
| Streamed outcome | `RunStreamAsync` — every event live, completion carries the structured outcome | 7.9 |
| Selection | `OnSelectTools((task, described) => offered)` | 7.10 |
| Enforced selection | `EnforceSelection()` | 7.10 |

These are not backends, so the three-verb rule does not govern them: a channel, a door, a
judgment delegate and a switch. Say that on camera rather than letting a viewer hunt for
`UseSelection(broker)` — a matrix that explains its own boundaries is the same promise as one
that names its gaps.

**Two rows worth calling out on camera because they look like cheats and are not:**

- **Tools** — Local and Custom are the same verb, because a tool you write *is* the custom mode.
  There is nothing to demonstrate twice; say so rather than inventing a distinction.
- **Trace** — External and Custom are the same verb, because `ILoggingBroker` is the whole seam:
  hand it a provider's implementation or your own class, and the framework cannot tell which.

---

## What "shown" means

For each capability, in its episode, all three run:

1. **Local first** — smallest thing that works, usually a path.
2. **External second** — swap in a provider package or a broker, **run it again, same result.**
3. **Custom third** — a delegate, inline, five lines. Run it a third time.

Then say the sentence that makes it land: *nothing else about the agent changed.* Diff the file on
camera where it's short enough to fit.

**Budget ~3 minutes per capability for the second and third modes.** That's the runtime cost of
this rule across the series, and it is the best-spent three minutes in each episode: it's the
difference between "here's a memory feature" and "here's a memory *seam*, and your Redis cluster
drops into it."

---

## Enforcement

`StandardAgentCapabilityTests` fails the build if any capability offers fewer than three modes, with
waivers written **in code, with their reason** — which is how the two Brain dashes stay honest
instead of becoming debt nobody tracks.

Show that test on screen in 1.7 and again in 8.8. It is the single best evidence that the triad is a
rule the project keeps rather than a claim it makes.
