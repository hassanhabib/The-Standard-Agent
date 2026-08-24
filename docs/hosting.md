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
| `POST api/agents/streams` | The same run as server-sent events — each event's kind as the SSE event name, its content as data. Filtering to `Response` events equals what `runs` returns. |

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

The agent is registered as a **singleton**, which is the intended shape
([support.md](support.md)): one instance serves prompts concurrently, and run state is per
invocation by SPEC.md §4.4. Enterprise controls — perimeter, budgets, sessions, redaction,
approval — are composition like everything else; add the builder calls where the singleton is
built.
