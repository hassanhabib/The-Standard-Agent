# Conformance Suite

A **language-neutral** set of behavioral test vectors that any Standard-Agents
implementation runs to self-certify against [`SPEC.md`](../SPEC.md).

## Why scripted

Agent behavior involves an LLM, which is non-deterministic — so it cannot be asserted
directly. Conformance instead tests the **deterministic** contracts: the loop, reply
interpretation, tool routing, and the feed-back of results into Data. It does this by
replacing the Brain with a **scripted generator** and tools with **stubs**, then
asserting on the one thing that is a stable cross-language contract: **the returned
result**.

## Vector schema (JSON)

```json
{
  "name": "tool-then-final",
  "description": "human-readable intent",
  "generatorReplies": ["ACTION: calculator: 1+1", "FINAL: 2"],
  "tools": { "calculator": "2" },
  "prompt": "what is 1+1",
  "expect": { "result": "2" }
}
```

- **generatorReplies** — the scripted Brain returns these in order, **repeating the last
  when exhausted** (so a single non-terminal reply exercises the turn cap).
- **tools** — stub *internal* tools: `name → fixed output`.
- **prompt** — the user task.
- **expect** — `{ "result": "<exact>" }` or `{ "resultContains": "<substring>" }`.

## Runner contract

To certify an implementation, provide a harness that, for each vector:

1. Wires a **scripted GeneratorBroker** returning `generatorReplies` in order (repeat the
   last when exhausted).
2. Registers `tools` as **internal** stub tools (each returns its fixed output).
3. Uses pass-through / stub brokers for everything else: Skill returns any text; Memory
   and Knowledge empty; Gate allows; Judge returns 1.0; External reports "not configured";
   Log is a no-op.
4. Runs the agent on `prompt` and compares the returned result to `expect`.

A conformant implementation **passes every vector**.

## Reference runner (C#)

```bash
dotnet run --project Standard.Agents.Conformance
```

Exit code `0` = all vectors pass. It reads the vectors from this folder and runs them
against the `Standard.Agents` reference library. Use it as the template for a runner in
your own language.

## Adding a language

Implement the harness (steps 1–4 above) in your language, point it at
`conformance/vectors/`, and run. If every vector passes, your implementation conforms to
the deterministic core of the Standard.

## What these vectors cover

| Vector | Verifies |
|---|---|
| `direct-answer` | A `FINAL` returns in one turn |
| `tool-then-final` | A tool result feeds back into Data and is used next turn |
| `first-line-action-only` | Only the first line's `ACTION` is parsed; extra lines ignored |
| `multiline-final` | A `FINAL` answer may span multiple lines |
| `unknown-tool-recovers` | An unknown tool routes to External and the agent recovers |
| `max-turns-cap` | A never-terminating Brain is capped by the loop |
| `structured-tool-call` | A structured `TOOL:` call (§6.1) routes to the tool with its arguments |
