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
| `POST api/V1/agents/runs` | The whole run: version 1 of the wire carries everything `PromptRequest` carries and answers with everything `AgentOutcome` reports, the pending effect included. The enterprise door; see below. |
| `POST api/V1/agents/streams` | The same V1 request, as server-sent events, framed exactly as `api/agents/streams`. |

Closing the connection cancels the run at its next turn boundary — and, through the nesting
seam, any sub-agents the run started.

The prompt-only `api/agents` routes are the convenience door and stay as they are: a prompt in,
a result and a status out. They are V0 of the wire and are not the enterprise API — a caller on
them cannot name a session, hand back a tool result, or see what a held run is waiting on.

## The whole run over the wire

Version 1 of the wire is `PromptRequest` and `AgentOutcome` in JSON, field for field. Every
field but `prompt` is optional; a list left out is empty, and a field set to `null` is refused
as `400` naming the field.

```json
POST api/V1/agents/runs
{
  "prompt": "and how many people live there?",
  "sessionId": "trip-3",
  "history": [ { "prompt": "capital of France?", "answer": "Paris." } ],
  "toolExchanges": [
    { "callId": "call_8f2", "toolName": "lookup", "argumentsJson": "{\"city\":\"Paris\"}", "result": "2.1M" }
  ],
  "callerTools": [
    { "name": "lookup", "description": "Looks a city up", "parametersJson": "{\"type\":\"object\"}" }
  ],
  "responseSchemaJson": null,
  "temperature": 0.2,
  "maxTokens": 400,
  "seed": 7,
  "stop": [],
  "providerOptionsJson": null
}
```

```json
{
  "result": "About 2.1 million.",
  "status": "Responded",
  "pendingEffect": null
}
```

What each field means is what it means on the contract ([how-to.md §18](how-to.md)): a session
wins over the caller's `history`; `callerTools` are vocabulary the model may name and never
capability the agent runs; inference fields the deployment configured win over the caller's.
Deliberately absent, as on the contract: executable tools, permissions, budget, redaction,
approvals, principal. The wire has no field in which to ask for them.

**The pending effect.** Only `Responded` makes the result an answer. A run that ends
`AwaitingApproval` or `AwaitingInput` carries the act it is waiting on, so a stateless caller
can see it, perform it, approve it, or answer it:

```json
{
  "result": "Holding the transfer until an authority answers.",
  "status": "AwaitingApproval",
  "pendingEffect": {
    "runId": "run-9",
    "callId": "",
    "toolName": "wire",
    "arguments": "{\"amount\":100}",
    "scope": "account:42",
    "riskLevel": "Irreversible",
    "approvalRequired": true,
    "idempotencyKey": "5b1e…",
    "principal": null
  }
}
```

**Continuing a held run is the same request.** Post the authority's decision, or the person's
reply, as the `prompt` on the same `sessionId`; the run picks up where it stopped, and an act
it already performed is recognised by its key and replayed rather than performed twice
(SPEC.md §4.9, §4.11). **Completing a caller's tool call is the same request too**: the run
ended `AwaitingInput` with the call as the pending effect, the caller ran it, and the result
comes back in `toolExchanges` naming the `callId` the model minted. There is no separate resume
route, because resuming is not a different operation; the session already holds everything.

**Identity is not established by this door.** The `X-Api-Key` gate below authenticates
possession of one shared secret and nothing more: it does not name a principal or a tenant, and
the wire carries none. A deployment that needs identity in policy, sessions and audit puts an
authenticating proxy or an ASP.NET authentication scheme in front of the host and resolves the
principal where the agent is composed (`.Principal(...)`, [how-to.md §12](how-to.md)); the wire
is deliberately not a place a caller can claim to be someone.

## Configuration

```json
{
  "Agent": {
    "Url": "http://localhost:11434/v1/",
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

**Memory is off in the host unless you turn it on.** One instance serves every caller, so a
memory file would be one memory for all of them: one caller's remembered facts in another
caller's context, and one caller able to poison memory for everyone after. The host composes
the agent without memory unless the document carries a `memory` key, or `Agent:Memory` names a
path in the classic configuration. Without one, the agent recalls nothing and never offers the
`remember` tool. Turning it on is a deliberate choice that every caller of that host shares.

**Keep the real document out of the repository.** An `agent.json` may carry an API key, so the
repository ignores `agent.json` and `agent.*.local.json`, and commits
`Standard.Agents.Host/agent.example.json` instead, without secrets. Copy the example to
`agent.json` beside the executable, set the key locally or point `Agent:Config` at a file your
secret store writes, and the ignore rule keeps a careless `git add .` from shipping it.

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

## HTTP out

The agent's own HTTP traffic (the brain, the native brain, every MCP server) rides handlers the
host owns. The host registers one named client, `standard-agent`, with `IHttpClientFactory` and
hands the agent its handlers through `.Http(...)`, so connections are pooled and DNS-refreshing
the way every other outbound call in an ASP.NET Core process is, and there is one place to put
a proxy, a certificate, a resilience handler or an observer under all of it:

```csharp
builder.Services.AddHttpClient("standard-agent")
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { Proxy = corporateProxy });
```

Ownership is explicit ([support.md](support.md)): handlers from the factory are the factory's,
the agent wraps them in clients that hold nothing of their own and never disposes them. The
library needs no package for this; the seam takes a `Func<HttpMessageHandler>`, and
`IHttpMessageHandlerFactory` is what the host passes.
