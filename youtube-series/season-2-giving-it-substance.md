# Season 2 — Giving It Substance

7 episodes · 9–14 min each · lands the **Core** profile

Season 1 built a chatbot. This season builds an *agent*: something with an identity, the ability to
act, a memory that outlives the process, and grounding in your data. Every episode adds exactly one
line and touches exactly one nature — mostly **Data**, once **Direction**.

By 2.7 the viewer can run a complete agent with no network connection anywhere.

---

## 2.1 — Skills: who the agent is, in Markdown

**Runtime** 14 min · **Branch** `series/s2e1-skills` · **Docs** how-to §2

**Cold open**
> "Your agent's personality does not belong in a C# string literal."

**Beats**
- `.Skills("Skills")` — point at a folder of Markdown.
- Write one on camera. Show it changing behaviour on the very next run with no rebuild of intent.
- Why Markdown and not code: the people who should own an agent's instructions are frequently not
  the people who can open a `.cs` file.
- The `{{tools}}` marker — how the tool catalogue expands into the prompt. (Tools land in 2.2; this
  is the forward reference that makes 2.2 feel inevitable.)
- Local / External / Custom for skills: a folder, the PeerLLM registry, or your own delegate.

**All three modes — all demonstrated (+3 min)**
- **Local** `.Skills("Skills")` — a folder of Markdown.
- **External** `.UseSkills(new PeerLLMSkillBroker("hassanhabib/my-skills", SkillSync.Hybrid))` — versioned skills pulled from the registry at runtime.
- **Custom** `.OnSkills(async () => await LoadSkillsFromMyCmsAsync())` — returns `IReadOnlyList<Skill>`.

Run all three. Same agent, same answer, three sources.

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

**Runtime** 12 min · **Branch** `series/s2e3-mcp` · **Docs** how-to §3

**Cold open**
> "There is an entire ecosystem of tools already built. You don't have to reimplement any of it."

**Beats**
- `.Mcp(...)` — Model Context Protocol servers as external tools.
- Connect a real MCP server, list what it exposes, call one.
- Internal tools vs external tools as two distinct foundations — and why that split exists rather
  than one "tools" bucket: they fail differently, and a failure should name which kind failed.
- Treat MCP output as what it is: **someone else's data entering your context.** Flag it hard, and
  point at 4.6 where it gets screened.

**All three modes — all demonstrated (+2 min)**
- **Local** — internal tools, from 2.2.
- **External** `.Mcp(endpointUrl, relativeUrl, timeoutSeconds)` — a server by URL.
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

**Runtime** 16 min · **Branch** `series/s2e5-knowledge` · **Docs** how-to §9

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

**All three modes — all demonstrated (+3 min)**
- **Local** `.Knowledge("Knowledge")` — a folder.
- **External** `.UseKnowledge(new PostgresKnowledgeBroker(cs))`, and the MsSql package as a second.
- **Custom** `.OnKnowledge(async query => await MyVectorStoreAsync(query))` — `Func<string, ValueTask<IReadOnlyList<string>>>`.

The Custom mode is the escape hatch for vector databases the framework ships no package for. Say so, because that is the question the comments will ask.

**The gotcha**
This vector was **vacuous when first written** — it passed with the ranking deliberately inverted,
and was caught by sabotage-testing and rewritten. Tell that story in sixty seconds. It teaches
viewers more about trusting a framework than any feature demo, and it's the honest history.

**What changed in the shape** — Data, third foundation. Data is now full at Core: Skill, Knowledge,
Memory.

---

## 2.6 — Swapping any nature's backend

**Runtime** 12 min · **Branch** `series/s2e6-backends` · **Docs** how-to "Swapping any nature's backend"

**Cold open**
> "Production doesn't run on text files. Here's the entire migration."

**Beats**
- Put the capability table on screen — Local / External / Custom for all sixteen.
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
