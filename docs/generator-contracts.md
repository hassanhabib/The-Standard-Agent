# The Two Generator Contracts

The agent can talk to a model in two ways. This is the whole difference between them, when each
one wins, and how to move.

Short version: **V0 is not deprecated and is not going away.** If you already have a working
agent, you do not have to do anything.

---

## What they are

**V0 — the text protocol.** One system prompt, one user prompt, one string back. The model's
choice is the first line of its reply:

```
ACTION: calculator: 47*89
FINAL: 4183
```

Optionally, at most one `SAY:` line may precede the choice — user-voiced progress narration
("Let me check the calculator…"). It is peeled before the first-line rule applies, screened by
the Gate, and voiced on the stream's `Narration` channel; it is never the act and never the
answer (SPEC.md §6.0):

```
SAY: Let me check the calculator...
ACTION: calculator: 47*89
```

`IGeneratorBroker`, reached through `.Brain(url, key, model)` or `.UseGenerator(broker)`.

**V1 — native tool calling.** A conversation of typed messages in; a structured choice out. The
model asks for a tool the way its provider trained it to, and the result comes back tied to the
call that asked for it.

`IGeneratorBrokerV1`, reached through `.UseNativeBrain(broker)` or `.OnNativeBrain(delegate)`.

---

## Which to use

| | V0 text protocol | V1 native calls |
|---|---|---|
| Works against | any endpoint that returns text | endpoints that expose tool calling |
| Tool choice | parsed from the first line | structured, typed |
| Result attribution | narrated back as prose | a tool message naming the call |
| Small local models | often **better** — a 3B model follows a format more reliably than it emits well-formed tool JSON | varies by model |
| Frontier hosted models | works, but forfeits what they were tuned for | **better** |

The honest rule: **use V1 when your provider supports it, V0 when it does not or when the model is
small.** A 3B GGUF running through LlamaSharp is usually happier imitating `ACTION:` than emitting
a schema-valid call, and pretending otherwise would make the framework worse on exactly the
hardware people run at home.

---

## Why the id matters

This is the one thing the text protocol cannot express, and it is the reason V1 exists at all.

When a model asks for `call_7` and the framework hands back:

```
Observations so far:
- calculator: 4183
```

…the model has to work out, by reading, which of its questions that answers. With one call in
flight that is trivial. With three it is guessing, and guessing is what native tool calling was
built to remove. V1 hands back what the model actually sent:

```
assistant  tool_calls: [ { id: "call_7", name: "calculator", arguments: {"expression":"47*89"} } ]
tool       tool_call_id: "call_7"   content: "4183"
```

Every outcome answers the call — a denial and a withheld result are answers too. A call left
unanswered strands the conversation, and some providers reject one whose tool call has no matching
tool message.

Certified by conformance vector `29-native-tool-call-round-trips`.

---

## Moving

One line:

```csharp
// before
new StandardAgent().Brain(apiUrl, apiKey, model)

// after
new StandardAgent().UseNativeBrain(new YourProviderNativeBroker(...))
```

### What does not change

Everything else. Adopting native calls changes how a *choice is read*, not what the agent is:

- The same tools, registered the same way. A tool does not know which contract called it.
- The same catalog rule: a description is the opt-in. A tool without one stays callable but is
  never offered to the model — natively exactly as in the text catalog.
- The same perimeter. Authorization, approval, run-once, screening and compensation all sit in
  Direction, after interpretation, and never see the difference.
- The same guardians, the same Judge, the same revision loop, the same trace and decision log.
- The same budget. Reported usage crosses both contracts, so a budget bounds a measurement rather
  than an estimate.
- The same redaction. Every message going out is redacted and every reply rehydrated, on both
  paths (SPEC.md §4.6).

### What you write

A V1 broker implements one method:

```csharp
ValueTask<GenerationResult> GenerateAsync(
    IReadOnlyList<ConversationMessage> messages,
    IReadOnlyList<ToolDefinition> tools);
```

Translate `messages` into your provider's request shape, translate its tool calls back into
`ModelToolCall(Id, Name, ArgumentsJson)`, and report `PromptTokens` / `CompletionTokens` if the
provider tells you. Report zero rather than an estimate: a budget that bounds a guess is not a
budget.

A broker MAY also populate `GenerationResult.Narration` — the `SAY:` line's native twin: one
line of user-voiced progress prose riding the structured result. The default is empty and
nothing requires it; when present it is screened and voiced through the same loop seam the text
protocol's narration uses. Certified by conformance vector `67-native-narration-rides-the-result`.

For a one-off, `.OnNativeBrain((messages, tools) => …)` takes the same shape as a delegate.

---

## Compatibility

- **V0 is not `[Obsolete]`.** It is the Core contract, because it is the one that works
  everywhere. Nothing in the deprecation policy (`docs/support.md`) is running against it.
- **Provider packages compile unchanged.** `Standard.Agents.Data.Memory.Redis` and its siblings
  implement other brokers entirely and are unaffected. A generator package written against V0
  keeps working; moving to V1 is additive, on its own schedule.
- **Configure one or the other.** An agent with a V1 brain uses native calls; an agent without one
  uses the text protocol. Composition requires *a* brain and rejects an agent with none, but it
  does not stop you configuring both — if you do, the native one is used and the text one is
  never called.
- The models are versioned side by side under `Models/Brokers/Generators/V1/`, so a future V2
  arrives the same way this one did: alongside, not on top.
