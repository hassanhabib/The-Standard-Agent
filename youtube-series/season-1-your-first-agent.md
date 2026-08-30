# Season 1 — Your First Agent

7 episodes · 6–9 min each · no profile yet, just a thing that talks

The season exists to answer one question a viewer arrives with: *is this going to be another
framework I have to learn?* The answer is a talking agent in under a minute, and then the same
agent switched from a hosted model to a local one by changing a single line. If they believe that
seam is real, they will stay for four more seasons.

---

## 1.1 — What an agent actually is

**Runtime** 7 min · **Branch** none (whiteboard + REPL)

**Cold open**
> "Every agent framework you've tried had a different set of nouns. Chains, graphs, runnables,
> executors. Here there are three, and you already know all of them."

**Beats**
- `Agent = Orchestration(Data, Decision, Direction)` on screen for the whole episode.
- **Data** — what it *has*: skills, memory, knowledge. Verb: **Recall**.
- **Decision** — what it *thinks*: one brain, wrapped in a Gate and a Judge. Verb: **Think**.
- **Direction** — what it *does*: act internally, act externally, or return. Verb: **Act**.
- Orchestration is not a fourth nature. It's the composition operator — the loop.
- The loop, drawn once: Recall → Think → Act, repeat until it returns.
- Show `assets/the-standard-agent-architecture.png` for *four seconds*, say "we'll earn this in
  season 6", and take it away.

**The gotcha**
"Tri-nature" sounds like taxonomy for its own sake until you see the payoff: because every capability
belongs to exactly one nature, you always know where a new feature goes, and you never have two
places that could own it.

**What changed in the shape** — nothing yet. This is the shape.

**Closing** — "Next: ten seconds, one line, a working agent."

---

## 1.2 — Ten seconds to a talking agent

**Runtime** 6 min · **Branch** `series/s1e2-hello` · **Docs** how-to §0

**Cold open**
> "No config file. No DI container. No YAML. Three arguments and a prompt."

**Beats**
- `dotnet new console`, `dotnet add package Standard.Agents`.
- The whole program:
  ```csharp
  var agent = new StandardAgent(apiUrl: "https://api.peerllm.com/v1/", apiKey: key, model: "LLooMA2.0");

  string answer = await agent.ProcessPromptAsync("What is 47 * 89?");
  ```
- Run it. Get an answer. That's the episode's promise, delivered at minute two.
- Then: what it *doesn't* have. No skills, no tools, no guardians, no memory. All opt-in, all one
  line each, all coming.
- Point at `Standard.Agents.Demo/` as a bigger runnable starting point.

**The gotcha**
No DI container is a deliberate design choice, not an omission — the composition root is hand-wired
and readable. Show that you *can* still register it in a container if your host has one; it's a
plain object.

**What changed in the shape** — Decision got a brain. Data and Direction are empty and the agent
still runs.

---

## 1.3 — Remote inference: the hosted brain

**Runtime** 8 min · **Branch** `series/s1e3-remote`

**Cold open**
> "Your prompt just left the building. Let's look at exactly what went with it."

**Beats**
- `.Brain(url, key, model)` — what the seam is: an HTTP call to an OpenAI-shaped endpoint.
- Turn on the trace (`.LogTo("log.txt")`) purely to *see* the call. Full observability is 4.3;
  here it's a debugging tool.
- What travels: system prompt + user message. What comes back: text.
- Latency, cost, and privacy as three separate axes — not one "cloud vs local" slider.
- Any OpenAI-compatible endpoint works: hosted providers, a gateway, vLLM, Ollama's compat API.

**The gotcha**
The hosted brain is the *External* mode of one capability, not the framework's foundation. It looks
foundational because it's in the constructor. It isn't — episode 1.4 removes it entirely.

**What changed in the shape** — nothing structural. Same Decision nature, one backend.

---

## 1.4 — Local inference: no network at all

**Runtime** 9 min · **Branch** `series/s1e4-local` · **Docs** how-to §1

**Cold open**
> "Same agent. Same code. Pull the network cable."

**Beats**
- ```bash
  dotnet add package Standard.Agents.Decision.Brains.LlamaSharp
  dotnet add package LLamaSharp.Backend.Cpu     # or .Cuda12 / .Vulkan
  ```
- ```csharp
  var agent = new StandardAgent()
      .UseGenerator(new LlamaSharpGeneratorBroker("path/to/model.gguf"));
  ```
- Where to get a GGUF, what quantisation means in one sentence, and pick something small enough
  that the viewer's laptop won't stall on camera.
- **Actually disable the network adapter on camera.** It's the whole point of the episode and it
  takes four seconds.
- CPU vs CUDA vs Vulkan backend packages — one line, and say when each is worth it.

**The gotcha — this is the big one, give it 90 seconds**
An empty reply from a local model is almost always the **prompt template**, not the model. The
package ships `PromptTemplates` — ChatML (default), Llama3, Nemotron and others. Demonstrate the
empty reply, then fix it by naming the template. This single minute will save more comments than
the rest of the season.

**What changed in the shape** — the Decision nature's broker. One line. Nothing else in the agent
knew it happened.

---

## 1.5 — Remote vs local, head to head

**Runtime** 9 min · **Branch** `series/s1e5-headtohead`

**Cold open**
> "Two agents. One line different. Let's find out what that line actually costs you."

**Beats**
- Same program, two configurations, side by side in one terminal.
- Measure, don't assert: first-token latency, total latency, and — using `.Budget()` and the trace
  — tokens consumed. (Full budget treatment is 4.7; here it's a measuring tape.)
- Honest scoring across five axes: **latency, cost, privacy, capability, operability.** Local wins
  privacy and marginal cost outright; hosted usually wins capability and operability. Say so.
- The hybrid that most people actually want: local brain, hosted guardians — or the reverse for a
  regulated deployment where the *data* is the sensitive part.
- Land the framework's actual claim: **you pick each nature's backend independently.** Local brain,
  cloud knowledge. Hosted brain, local memory. The seam is per-nature.

**The gotcha**
A small local model will fail the *reply protocol* more often than a hosted one — it parrots the
`ACTION:` template without a tool name behind it. The framework already handles that specific case
by treating it as an answer rather than routing an empty tool name into Direction; show the trace
line where that happens so it isn't mistaken for the model being broken.

**What changed in the shape** — nothing. That's the episode.

---

## 1.6 — Streaming

**Runtime** 8 min · **Branch** `series/s1e6-streaming`

**Cold open**
> "Waiting eleven seconds for a wall of text is a product bug, not a model limitation."

**Beats**
- ```csharp
  await foreach (AgentStreamEvent streamEvent in agent.StreamPromptAsync("What is 47 * 89?"))
  {
      switch (streamEvent.Type)
      {
          case AgentStreamEventType.Thinking: /* deliberating / tool reasoning */ break;
          case AgentStreamEventType.Response: /* the answer, token by token */    break;
          case AgentStreamEventType.Tool:     /* a tool ran, and its result */    break;
          case AgentStreamEventType.Status:   /* lifecycle: turns, gate, judge */ break;
      }
  }
  ```
- Wire it to a console with each type rendered differently. Four events, four colours.
- **Why a draft streams as `Thinking` and not `Response`:** the answer isn't an answer until the
  Judge has settled it. Filtering a stream to `Response` therefore equals exactly what
  `ProcessPromptAsync` returns. Demo it — that parity is a promise the framework keeps and most
  frameworks don't.

**The gotcha**
Every control the batched loop enforces — cancellation, budgets, sessions, compensation — is
enforced on the streamed loop too. A control a caller can step around by changing method is not a
control. This was a real defect once; it's worth ten seconds of "and here's why you can trust that."

**What changed in the shape** — nothing. Same loop, second door.

---

## 1.7 — Bring your own runtime

**Runtime** 10 min · **Branch** `series/s1e7-onbrain`

**Cold open**
> "You already have inference wired up. You shouldn't have to throw it away to use this."

**Beats**
- ```csharp
  var agent = new StandardAgent()
      .OnBrain((systemPrompt, userPrompt) => RunMyLocalModelAsync(systemPrompt, userPrompt));
  ```
- Wire it to something real — ONNX Runtime, a subprocess, an in-house client.
- Introduce the naming rule that governs the entire rest of the series, because from here on it
  explains every API the viewer will meet:
  - **`.X(...)`** — Local. Point at something in the box.
  - **`.UseX(broker)`** — External. A provider package.
  - **`.OnX(delegate)`** — Custom. Your own code, inline.
- Sixteen capabilities, the same three verbs each, and a **test fails the build** if a capability
  offers fewer. Show `StandardAgentCapabilityTests` for five seconds — it lands better as evidence
  than as a claim.
- Why Brain has no Local mode: running a model in-process needs an inference runtime, and the core
  is dependency-free by design. It's a documented impossibility with a reason, not a gap.

**All three modes — the Brain triad closed, and the only gap in the framework named (+3 min)**

Season 1 has been demonstrating one capability across three episodes; put them side by side here:
- **Local** — **none.** One of only two dashes in the whole matrix. *Local* means in the box with no
  dependency, and running a model in-process needs an inference runtime. It is a **documented
  impossibility with a reason**, not debt — and the reason is written into the waiver in
  `StandardAgentCapabilityTests`, which is what stops it quietly becoming debt later.
- **External** `.Brain(url, key, model)` for an endpoint (1.3), or `.UseGenerator(broker)` for any
  `IGeneratorBroker` — which is how the LlamaSharp package gives you a local brain in one line (1.4).
- **Custom** `.OnBrain((systemPrompt, userPrompt) => ...)` — your runtime, inline.

Put the waiver on screen and read it aloud. A framework that names its two gaps and explains them is
making a different kind of promise than one that leaves you to discover them.

**The gotcha**
`.OnBrain` is *Custom*, not Local — a delegate you write is your code, not something in the box.
The methods were originally misnamed `LocalBrain` / `LocalGate` / `LocalJudge`; they're now
`.OnBrain` / `.OnGate` / `.OnJudge`, with the old names kept as `[Obsolete]` aliases. If a viewer
finds the old names in a blog post, this is why.

**What changed in the shape** — Decision, again, and for the third distinct backend in one season.

**Season close** — "It talks, and you can host its brain anywhere. Next season it stops being a
chatbot: skills, tools, memory, and knowledge — everything the agent *has*."
