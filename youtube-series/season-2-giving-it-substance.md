# Season 2 — Giving It Substance

7 episodes · 9–14 min each · lands the **Core** profile

Season 1 built a chatbot. This season builds an *agent*: something with an identity, the ability to
act, a memory that outlives the process, and grounding in your data. Every episode adds exactly one
line and touches exactly one nature — mostly **Data**, once **Direction**.

By 2.7 the viewer can run a complete agent with no network connection anywhere.

---

## 2.1 — Skills: who the agent is, in Markdown

**Runtime** 15 min · **Branch** `series/s2e1-skills` · **Docs** how-to §2

**Cold open**
> "Your agent's personality does not belong in a C# string literal."

**Beats**
- `.Skills("Skills")` — point at a folder of Markdown. Every `.md` under it, **subfolders
  included**, loads in path order — so `the-standard-skill/SKILL.md` works exactly as a flat file
  does.
- Write one on camera. Show it changing behaviour on the very next run with no rebuild of intent.
- Why Markdown and not code: the people who should own an agent's instructions are frequently not
  the people who can open a `.cs` file.
- YAML frontmatter (`name:` / `description:`) is stripped from what the brain sees and becomes the
  skill's index entry. Then the `{{skills}}` marker: one `- name — description` line per
  **described** skill, so the model knows which specialist skill it has and reaches for it.
- The `{{tools}}` marker — the same idea for the tool catalogue. (Tools land in 2.2; this is the
  forward reference that makes 2.2 feel inevitable.)
- Local / External / Custom for skills: a folder, the PeerLLM registry, or your own delegate.

**All three modes — all demonstrated (+3 min)**
- **Local** `.Skills("Skills")` — a folder of Markdown.
- **External** `.UseSkills(new PeerLLMSkillBroker("hassanhabib/my-skills", SkillSync.Hybrid))` — versioned skills pulled from the registry at runtime (`Standard.Agents.Data.Skills.PeerLLM`).
- **Custom** `.OnSkills(async () => await LoadSkillsFromMyCmsAsync())` — returns `IReadOnlyList<Skill>`.

Run all three. Same agent, same answer, three sources. **Then keep all three.** Sources
accumulate: a second `.Skills`, `.UseSkills` or `.OnSkills` *adds* a source rather than replacing
the first, and the skills concatenate in registration order — so the team's registry skillset
sits beside the local folder that says who this particular agent is.

```csharp
var agent = new StandardAgent(url, key, "LLooMA2.0")
    .Skills("Skills")                          // who this agent is
    .UseSkills(new PeerLLMSkillBroker(         // what the team knows
        "hassanhabib/my-skills",
        SkillSync.Hybrid));
```

**The gotcha**
Skills are **Data**, not Decision. They're something the agent *has*, not something it *thinks*.
That distinction decides where every future feature goes, and it's the first time in the series it
does real work.

**What changed in the shape** — Data gained its first foundation.

---

## 2.2 — Tools: what it can actually do

**Runtime** 15 min · **Branch** `series/s2e2-tools` · **Docs** how-to §3

**Cold open**
> "An agent that can only talk is a very expensive autocomplete."

**Beats**
- Implement `ITool`: `Name`, `Description`, `Parameters`, `ExecuteAsync`.
- `.Tool(new CalculatorTool())`, and `.Tools(...)` for the batch form.
- Watch a turn in the trace: Recall → Think (model picks the tool) → Act (tool runs) → the result
  comes back as an observation → next turn.
- The text protocol on screen: `ACTION: calculator: 1+1`. Show the raw reply so the viewer knows
  there is no magic — just a parsed first line. (Native tool calling is 5.3.)
- `.MaxTurns(n)` — the turn budget shared across tool calls and Judge revisions. Default 7.

**All three modes — all demonstrated (+2 min)**
- **Local** `.Tool(new CalculatorTool())`, and `.Tools(...)` for the batch form.
- **External** `.Mcp("https://…")` — covered properly in 2.3.
- **Custom** `.Tool(new MyTool())` — the same verb, because a tool you write *is* the custom mode.

Say the Tools row out loud: Local and Custom share a verb, and that is honest rather than a gap.

**When the model gets the protocol wrong (+2 min)**

Small models fumble the reply format constantly, and the framework has tested contracts for it —
show both, because a viewer who hits these will otherwise assume the framework is broken:
- **`unknown-tool-recovers`** — the model asks for a tool that does not exist. The agent is told so
  and re-thinks, rather than faulting. Register one tool, prompt for another, watch it recover.
- **`multiline-final`** — an answer that spans several lines is still one answer. The parser reads
  the *first line* for an intent, and everything after `FINAL:` is the reply.
- The empty `ACTION:` case from 1.5: the prefix with no tool name behind it is treated as an answer
  rather than routed into Direction as an empty tool name.

**The gotcha**
**A description is the opt-in.** A tool with no description stays callable but is never advertised
to the model. That's deliberate — it's how you keep a tool reachable by your own code without
widening what the model may reach for. Demonstrate both halves.

**What changed in the shape** — Direction gained tools. First episode of the season that isn't Data.

---

## 2.3 — MCP: tools you didn't write

**Runtime** 13 min · **Branch** `series/s2e3-mcp` · **Docs** how-to §3

**Cold open**
> "There is an entire ecosystem of tools already built. You don't have to reimplement any of it."

**Beats**
- `.Mcp(...)` — Model Context Protocol servers as external tools.
- Connect a real MCP server, list what it exposes, call one. Build it with an official SDK on
  camera: the agent speaks the Streamable HTTP transport (event-stream replies, the
  `initialize` handshake, the session), so the server needs nothing special for it.
- **Then the ones that run as a process.** `.McpProcess("npx", [...])` starts a local server and
  speaks to it over standard input and output — the transport most of the ecosystem ships. The
  same entry pastes into an agent document as `{ "command": "npx", "args": [...] }`.
- **Then connect a second.** `.Mcp(...)` accumulates: the agent asks each server for its
  `tools/list` catalog and routes every call to the server that owns the name. Two servers
  claiming one name — the **first registered wins**, the same precedence local tools already have.
  Stop one server on camera: only *its* tools go unavailable, and it is asked again on the next
  call rather than cached as "has no tools".
- Auth is per server and optional: nothing, an `apiKey` in a header you can rename, a
  `bearerToken`, or a `bearerTokenProvider` delegate asked before every request for OAuth refresh
  flows. Your OAuth client runs the flow; the agent carries the result.
- Internal tools vs external tools as two distinct foundations — and why that split exists rather
  than one "tools" bucket: they fail differently, and a failure should name which kind failed.
- Treat MCP output as what it is: **someone else's data entering your context.** Flag it hard, and
  point at 4.6 where it gets screened.

**All three modes — all demonstrated (+2 min)**
- **Local** — internal tools, from 2.2.
- **External** `.Mcp(endpointUrl, relativeUrl, timeoutSeconds)` — a server by URL;
  `.McpProcess(command, arguments, environmentVariables)` — a server the agent starts.
- **Custom** `.UseMcp(new MyMcpBroker(...))` — your own transport, auth, or in-process stub.

The stub is not a toy: it is how you test MCP integration without a live server (7.7).

**The gotcha**
Every MCP tool is a third-party dependency with network access and its own failure modes. The
framework will let you register a dozen; that doesn't make it wise. This is the first appearance of
the perimeter mindset that season 4 is built on.

**What changed in the shape** — Direction, external side.

---

## 2.4 — Memory: it remembers you across restarts

**Runtime** 14 min · **Branch** `series/s2e4-memory` · **Docs** how-to §8

**Cold open**
> "Tell it your name. Restart it. It has no idea who you are."

**Beats**
- Demonstrate the amnesia first. Always demonstrate the failure first.
- `.Memory("memory.txt")` — one line, and the amnesia is gone.
- The built-in `remember` tool: the agent decides what's worth keeping. Show it choosing.
- Open `memory.txt` on camera. It's a text file. That transparency is a feature — you can read,
  edit, and delete what your agent knows about a user.
- Swap to Redis in one line with `Standard.Agents.Data.Memory.Redis`, keyed per agent/user/session.

**All three modes — all demonstrated (+3 min)**
- **Local** `.Memory("memory.txt")` — a text file you can open on camera.
- **External** `.UseMemory(new RedisMemoryBroker(redis))` — keyed per agent / user / session.
- **Custom** `.OnMemory(...)` — read and write against whatever store you already run.

Run the same "remember my name" flow through all three, restarting between each.

**The gotcha**
Memory (facts that persist across conversations) is **not** conversation history (this dialogue).
They're different foundations with different lifetimes, and conflating them is the most common
design error in agent apps. Sessions are 4.8.

**What changed in the shape** — Data, second foundation.

---

## 2.5 — Knowledge: grounding on your data

**Runtime** 21 min · **Branch** `series/s2e5-knowledge` · **Docs** how-to §9

**Cold open**
> "It's confidently wrong about your product because it has never read your docs."

**Beats**
- `.Knowledge("Knowledge")` — point at a folder.
- Ask a question only the docs can answer. Before and after.
- **Ranked by relevance, not first-found.** This is a real, tested contract
  (`knowledge-retrieves-by-relevance`), not a nice-to-have. Show a case where first-found gives the
  wrong passage and ranking gives the right one.
- Scale up: Postgres (`tsvector`) and SQL Server (`FREETEXT`) packages, one line each.
- `KnowledgeMaxResults` — why the default is small, and what happens to cost and precision when
  you raise it.

**Where did that answer come from? (+5 min)**
- The second question a grounded agent gets, from a bank's compliance team, a helpdesk lead, a
  developer reading an answer about an API: *which document said that?* Ask the model to cite and
  watch it cite when it feels like it. A citation the model may or may not write is not a citation.
- `KnowledgeResult { Text, Score, Source }` — a passage, the score its source ranked it by, and a
  source a reader can follow back: a title, a path, an address. A score means something only
  beside the same source's other scores; a full-text rank and a vector distance are different
  scales.
- The folder is citable out of the box: each passage is sourced by its path relative to the
  folder, forward slashes on every platform.
- `.CiteKnowledge()` — and the answer ends with `Source: policies/refunds.md`. The model never wrote
  that line; the agent did, from the sources Recall put on the run. Show the Brain's prompt: the
  passage reads exactly as it did before citation existed. The source travels beside it.
- The rules, each one on screen:
  - **Off unless asked.** An agent that never called it answers byte for byte as before.
  - **After the Judge.** The Judge scores the model's words, not lines the agent added.
  - **Only an answer is cited.** A refusal, a question back, a held act or a failure credits
    nothing. Neither does an answer held to a response schema: a line after JSON breaks the JSON.
  - **Once per source,** in the order recalled, skipping one the answer already credits.
  - **One answer on every door** — the batched result, the streamed response, the streamed outcome
    and the session's record carry the same text.
  - **The deployment decides first.** `.CiteKnowledge()` cites for every caller,
    `.CiteKnowledge(cite: false)` forbids it, and an agent that said nothing lets each request
    decide with `PromptRequest.CiteKnowledge` — or `citeKnowledge` on the Host's V1 run request.
    In the document: `"citeKnowledge": true`, `false`, or the line's prefix.
- What a citation claims, and say it out loud: **the source was recalled into the run that
  answered.** Not that the model relied on it. Honest about the one thing it can know.

**All three modes — all demonstrated (+3 min)**
- **Local** `.Knowledge("Knowledge")` — a folder. Sourced by path.
- **External** `.UseKnowledge(new PostgresKnowledgeBroker(cs))`, and the MsSql package as a second.
  Both return plain passages, so they keep working and are never cited. A store that knows a row's
  title or URL implements `ISourcedKnowledgeBroker` instead and joins with the same
  `.UseKnowledge(broker)`.
- **Custom** `.OnKnowledge(async query => await MyVectorStoreAsync(query))` — `Func<string, ValueTask<IReadOnlyList<string>>>`.
  The sourced form is `.OnSourcedKnowledge(async query => ...)`, returning `KnowledgeResult`s.

The Custom mode is the escape hatch for vector databases the framework ships no package for. Say so, because that is the question the comments will ask. Then show the sourced form, because the next question is *and can it cite them?*

**The gotcha**
This vector was **vacuous when first written** — it passed with the ranking deliberately inverted,
and was caught by sabotage-testing and rewritten. Tell that story in sixty seconds. It teaches
viewers more about trusting a framework than any feature demo, and it's the honest history.

The second gotcha is the reason citation exists at all. A deployment asked its model, in the
instructions, to end each grounded answer with its source. It mostly didn't. The fix was not a
better instruction; it was moving the citation out of the model's hands. Whenever a behaviour
matters, ask who is guaranteeing it — and if the answer is "the model, usually", it is not
guaranteed.

**What changed in the shape** — Data, third foundation. Data is now full at Core: Skill, Knowledge,
Memory.

---

## 2.6 — Swapping any nature's backend

**Runtime** 12 min · **Branch** `series/s2e6-backends` · **Docs** how-to "Swapping any nature's backend"

**Cold open**
> "Production doesn't run on text files. Here's the entire migration."

**Beats**
- Put the capability table on screen — Local / External / Custom for all twenty.
- Live migration, one line at a time, running the agent between each:
  ```csharp
  var agent = new StandardAgent(url, key, "LLooMA2.0")
      .UseSkills(new PeerLLMSkillBroker("hassanhabib/my-skills", SkillSync.Hybrid))
      .UseKnowledge(new PostgresKnowledgeBroker(connectionString))
      .UseMemory(new RedisMemoryBroker(redis));
  ```
- **Nothing else in the agent changes.** Diff the file on camera to prove it.
- The two dashes in the table — Brain and Native brain have no Local mode. Documented
  impossibilities with a stated reason, not debt. Contrast with a framework that just leaves gaps.

**The gotcha**
Mix freely and deliberately: a local brain with cloud knowledge, or a registry of skills with a
Redis memory. Each swap changes one nature. The temptation is to migrate everything at once because
it's easy; the reason not to is that you lose the ability to attribute a regression.

**What changed in the shape** — three backends, zero structure.

---

## 2.7 — The fully local, fully offline agent

**Runtime** 12 min · **Branch** `series/s2e7-offline` · **Docs** how-to §10

**Cold open**
> "Airplane mode. Full agent. Skills, memory, knowledge, guardians, all of it."

**Beats**
- One GGUF drives brain, gate and judge — one model instance, three rubrics:
  ```csharp
  var llama = new LlamaSharpGeneratorBroker("model.gguf");

  var agent = new StandardAgent()
      .UseGenerator(llama)             // brain     — local
      .OnGate(llama.GenerateAsync)     // gate      — local, same model
      .OnJudge(llama.GenerateAsync)    // judge     — local, same model
      .Skills("Skills")
      .Memory("agent-memory.txt")      // memory    — local file
      .Knowledge("Knowledge")          // knowledge — local folder
      .LogTo("log.txt");
  ```
- Network off, on camera, for the whole demo.
- This is the **collapsible substrate**: the public API, the loop and the Tri-Nature never change;
  power lives in the brokers and the deployment.
- Where this genuinely matters: air-gapped networks, regulated data that cannot leave, edge devices,
  and demos at conferences with hostile wifi.
- Gate and Judge appear here as *configuration*, deliberately unexplained. Season 3 is next.

**The gotcha**
One model wearing three hats is cheap but correlated: a brain that is wrong in a particular way is
often a judge that is wrong in the same way. It's a legitimate deployment and a real weakness. Name
it, and forward-reference 3.6, where a *distinct* conscience is the fix.

**Season close** — "It has substance. It has no judgement. Next season it learns to say no."
