# Hosting

The same agent definition as a service. `Standard.Agents.Host` is the exposure layer as The
Standard defines one — controllers that are pure mapping over one dependency (`IAgent`), a
heartbeat, and composition that is configuration rather than code. Nothing was added to the
builder to make this possible: the appliance stays five lines, and the host is a consumer of
them like any other.

```bash
dotnet run --project Standard.Agents.Host
```

## Endpoints

| Endpoint | What it does |
|---|---|
| `GET api/home` | Aliveness, nothing else — no security, no dependencies. What a load balancer checks. |
| `POST api/agents/runs` | `{ "prompt": "..." }` → `{ "result": "...", "status": "Responded" }`. Status travels beside result because only `Responded` makes the result an answer. An empty prompt is `400` before any run starts. |
| `POST api/agents/streams` | The same run as server-sent events — each event's kind as the SSE event name (`Status`, `Thinking`, `Narration`, `Tool`, `Response`), its content as data, one `data:` line per line of content, which any SSE client joins back with a newline. Filtering to `Response` events equals what `runs` returns. |

Closing the connection cancels the run at its next turn boundary — and, through the nesting
seam, any sub-agents the run started.

## Configuration

```json
{
  "Agent": {
    "Url": "http://localhost:11434/v1/chat/completions",
    "ApiKey": "",
    "Model": "LLooMA2.0",
    "Skills": "Skills"
  }
}
```

Zero config still runs: without a Brain the host stands, heartbeats, and answers every run
with what to configure — a first deployment fails loudly at the first prompt, never silently
at startup.

A document that refuses to compose (an unknown key, a cost budget with no rate) behaves
differently from a missing brain: the agent is composed on the first request, not at startup,
so the host still stands and heartbeats, and every prompt is a 500 whose log line names the
offending entry. A green heartbeat is not proof the document composed; the first prompt is.

The agent is registered as a **singleton**, which is the intended shape
([support.md](support.md)): one instance serves prompts concurrently, and run state is per
invocation by SPEC.md §4.4. Enterprise controls — perimeter, budgets, sessions, redaction,
approval — are composition like everything else; add the builder calls where the singleton is
built.

## The agent as a document

The host also composes its agent from a single JSON document — the whole configurable surface
as data ([how-to.md §16](how-to.md)). Point `Agent:Config` at the file, or just drop an
`agent.json` beside the executable:

```json
{
  "brain": { "apiUrl": "https://api.peerllm.com/v1/", "apiKey": "k", "model": "LLooMA2.0" },
  "skills": "Skills",
  "ruleGate": ["password"],
  "budget": { "maxCostUsd": 0.25, "costPerThousandTokens": 0.002 },
  "telemetry": "form-built-agent"
}
```

When the document is present it is the whole truth — skills, guardians, budgets and telemetry
come from its keys, never from a second config source that could quietly disagree. A document
with no brain key still stands and heartbeats, and answers every run with exactly what to add,
rather than failing at startup or at the first prompt. No document means the classic
`Agent:Url` configuration above, unchanged.

## Locking the door

An agent endpoint carries approval and budget semantics, so the front door is worth a thought.
One configuration line locks it:

```json
{ "Host": { "ApiKey": "psk-your-key" } }
```

With a key configured, every agent route requires an `X-Api-Key` header carrying it — compared
fixed-time on the bytes, so a mismatch cannot leak the key one character at a time — and answers
`401` otherwise. The `api/home` heartbeat stays open either way: a probe cannot present a key,
and aliveness tells an attacker nothing. No key configured means open, which is what a laptop
wants and exactly what every release before this one did.

## Telemetry out

The hosted agent composes `.Telemetry(Agent:Name)`, so every run and turn is a span and every
token is metered, named by the OTel GenAI semantic conventions. The host wires the
OpenTelemetry SDK — the agent's source and meter plus ASP.NET Core instrumentation — behind the
standard switch:

```bash
OTEL_EXPORTER_OTLP_ENDPOINT=http://collector:4317 dotnet run --project Standard.Agents.Host
```

Set the endpoint and traces and metrics leave over OTLP for your collector; leave it unset and
nothing is wired at all. The exporter lives here rather than in the library deliberately: the
core emits through the BCL's `ActivitySource` with zero dependencies, and shipping spans
somewhere is a deployment concern — exactly what a host is for.
