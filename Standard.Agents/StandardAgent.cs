// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using Standard.Agents.Brokers.Classifiers;
using Standard.Agents.Brokers.Files;
using Standard.Agents.Brokers.Generators;
using Standard.Agents.Brokers.Knowledges;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Brokers.Mcps;
using Standard.Agents.Brokers.Memorys;
using Standard.Agents.Brokers.Skills;
using Standard.Agents.Brokers.Times;
using Standard.Agents.Brokers.Tools;
using Standard.Agents.Brokers.Verifiers;
using Standard.Agents.Models.Clients.Agents;
using Standard.Agents.Models.Foundations.Brains;
using Standard.Agents.Models.Loggings;
using Standard.Agents.Prompts;
using Standard.Agents.Services.Coordinations;
using Standard.Agents.Services.Foundations.Brains;
using Standard.Agents.Services.Foundations.ExternalTools;
using Standard.Agents.Services.Foundations.Gates;
using Standard.Agents.Services.Foundations.InternalTools;
using Standard.Agents.Services.Foundations.Judges;
using Standard.Agents.Services.Foundations.Knowledges;
using Standard.Agents.Services.Foundations.Memorys;
using Standard.Agents.Services.Foundations.Returns;
using Standard.Agents.Services.Foundations.Skills;
using Standard.Agents.Services.Orchestrations.Data;
using Standard.Agents.Services.Orchestrations.Decision;
using Standard.Agents.Services.Orchestrations.Direction;
using Standard.Agents.Tools;

namespace Standard.Agents;

public sealed partial class StandardAgent : IAgent
{
    private readonly List<ITool> tools = [];

    private string skillsPath = "Skills";
    private string constitutionPath = string.Empty;
    private string consumptionPath = string.Empty;
    private string logPath = string.Empty;
    private string auditPath = string.Empty;
    private IEnumerable<RedactionRule>? redactionRules;
    private IEnumerable<string>? allowedTools;
    private TraceVerbosity traceVerbosity = TraceVerbosity.Full;
    private string memoryPath = "memory.txt";
    private int maxTurns = 7;
    private string knowledgePath = "Knowledge";
    private string knowledgePattern = "*.md";
    private int knowledgeMaxResults = 3;

    private InferenceSettings? brainSettings;
    private InferenceSettings? gateSettings;
    private InferenceSettings? judgeSettings;

    private string mcpEndpointUrl = string.Empty;
    private string mcpRelativeUrl = string.Empty;
    private int mcpTimeoutSeconds = 30;

    private ISkillBroker? skillBroker;
    private IGeneratorBroker? generatorBroker;
    private IMemoryBroker? memoryBroker;
    private IKnowledgeBroker? knowledgeBroker;
    private IClassifierBroker? classifierBroker;
    private IVerifierBroker? verifierBroker;
    private Func<string, string, ValueTask<string>>? localGateScreen;
    private Func<string, string, ValueTask<string>>? localJudgeEvaluate;
    private IMcpBroker? mcpBroker;
    private ILoggingBroker? loggingBroker;

    private IAgentCoordinationService? agent;

    /// <summary>
    /// Creates an agent with nothing configured yet — set it up with the builder methods
    /// (<see cref="Brain"/>, <see cref="LocalBrain"/>, <see cref="Skills"/>, and the rest)
    /// before processing a prompt.
    /// </summary>
    public StandardAgent()
    {
    }

    /// <summary>
    /// Creates a ready-to-run agent against an OpenAI-compatible endpoint — the simplest start,
    /// the same as <c>new StandardAgent().Brain(apiUrl, apiKey, model)</c>. Chain further builder
    /// methods afterward to add skills, tools, guardians, memory or knowledge.
    /// </summary>
    /// <param name="apiUrl">Base URL of the OpenAI-compatible endpoint.</param>
    /// <param name="apiKey">API key for the endpoint (empty string if none is needed).</param>
    /// <param name="model">Model name to request from the endpoint.</param>
    public StandardAgent(string apiUrl, string apiKey, string model) =>
        Brain(apiUrl, apiKey, model);

    /// <summary>
    /// Points the agent at a folder of <c>.md</c> skill files — the prompts-as-Data that
    /// shape how it thinks (SPEC.md §7.2). The files must be copied to the build output.
    /// A skill containing the <c>{{tools}}</c> marker is where advertised tools are listed.
    /// </summary>
    /// <param name="path">Folder holding the <c>.md</c> skill files.</param>
    /// <returns>The same agent, so calls can be chained.</returns>
    public StandardAgent Skills(string path) =>
        Set(() => this.skillsPath = path);

    /// <summary>
    /// Points the agent at the ethical constitution: a markdown file whose text is prepended
    /// to every guardian rubric (Gate and Judge), above the built-in policy, so the guardians
    /// are bound by it. It takes effect only when a guardian is configured, and it never
    /// replaces the built-in output contract. Omit it to run the built-in guardian policy
    /// alone. The file must be copied to the build output.
    /// </summary>
    /// <param name="path">Path to the constitution <c>.md</c> file.</param>
    /// <returns>The same agent, so calls can be chained.</returns>
    public StandardAgent Constitution(string path) =>
        Set(() => this.constitutionPath = path);

    /// <summary>
    /// Points the agent at the consumption skill: a markdown file whose text replaces the
    /// built-in guardian policy (what the Gate screens for and what the Judge scores). The
    /// built-in output contract is always kept, so a replacement policy cannot break the
    /// guardian's wiring. It sits below the constitution and above the contract, and takes
    /// effect only when a guardian is configured. Omit it to use the built-in policy. The
    /// file must be copied to the build output.
    /// </summary>
    /// <param name="path">Path to the consumption skill <c>.md</c> file.</param>
    /// <returns>The same agent, so calls can be chained.</returns>
    public StandardAgent Consumption(string path) =>
        Set(() => this.consumptionPath = path);

    /// <summary>
    /// Sets the brain: an external, OpenAI-compatible chat-completions endpoint that does the
    /// agent's reasoning. Required unless you supply an in-process brain via
    /// <see cref="LocalBrain"/> or <see cref="UseGenerator"/>.
    /// </summary>
    /// <param name="apiUrl">Base URL of the OpenAI-compatible endpoint.</param>
    /// <param name="apiKey">API key for the endpoint (empty string if none is needed).</param>
    /// <param name="model">Model name to request from the endpoint.</param>
    /// <param name="temperature">Sampling temperature; higher is more varied. Defaults to 0.7.</param>
    /// <param name="maxTokens">Maximum tokens to generate per turn. Defaults to 1024.</param>
    /// <param name="timeoutSeconds">Per-request timeout in seconds. Defaults to 120.</param>
    /// <returns>The same agent, so calls can be chained.</returns>
    public StandardAgent Brain(
        string apiUrl,
        string apiKey,
        string model,
        double temperature = 0.7,
        int maxTokens = 1024,
        int timeoutSeconds = 120) =>
        Set(() => this.brainSettings =
            new InferenceSettings(apiUrl, apiKey, model, temperature, maxTokens, timeoutSeconds));

    /// <summary>
    /// Supplies an in-process brain as a delegate, so the agent makes no API calls — the
    /// local counterpart to <see cref="Brain"/>. Pick one, a local brain or an external one.
    /// For a runtime that streams natively, implement <c>IGeneratorBroker</c> and pass it to
    /// <see cref="UseGenerator"/> instead.
    /// </summary>
    /// <param name="generate">
    /// A <c>(systemPrompt, userPrompt) =&gt; answer</c> delegate that produces one reply.
    /// </param>
    /// <returns>The same agent, so calls can be chained.</returns>
    [Obsolete("Renamed to OnBrain: a delegate is the Custom mode, not the Local one. " +
        "Local means a provider that runs on your own machine. This alias keeps working.")]
    public StandardAgent LocalBrain(Func<string, string, ValueTask<string>> generate) =>
        Set(() => this.generatorBroker = new FunctionGeneratorBroker(generate));

    /// <summary>
    /// Supplies your own brain as a delegate — the <b>Custom</b> mode, the open override for when
    /// neither the built-in nor a provider package fits. The agent makes no API calls; you supply
    /// the inference. For a runtime that streams natively, implement <c>IGeneratorBroker</c> and
    /// pass it to <see cref="UseGenerator"/> instead.
    /// </summary>
    /// <param name="generate">
    /// A <c>(systemPrompt, userPrompt) =&gt; answer</c> delegate that produces one reply.
    /// </param>
    /// <returns>The same agent, so calls can be chained.</returns>
    public StandardAgent OnBrain(Func<string, string, ValueTask<string>> generate) =>
        Set(() => { });

    /// <summary>
    /// Turns on the Gate using an in-process model — the local counterpart to <see cref="Gate"/>,
    /// with no API calls. The delegate receives the built-in gate rubric as the system prompt and
    /// the prompt to screen as the user prompt, and returns the verdict. Pass a local brain's
    /// GenerateAsync to let one local model serve as both brain and gate.
    /// </summary>
    /// <param name="screen">A <c>(gateRubric, prompt) =&gt; verdict</c> delegate.</param>
    /// <returns>The same agent, so calls can be chained.</returns>
    public StandardAgent LocalGate(Func<string, string, ValueTask<string>> screen) =>
        Set(() => this.localGateScreen = screen);

    /// <summary>
    /// Turns on the Judge using an in-process model — the local counterpart to <see cref="Judge"/>,
    /// with no API calls. The delegate receives the built-in judge rubric as the system prompt and
    /// the draft answer as the user prompt, and returns the score. Pass a local brain's GenerateAsync
    /// to let one local model serve as both brain and judge.
    /// </summary>
    /// <param name="evaluate">A <c>(judgeRubric, draftAnswer) =&gt; score</c> delegate.</param>
    /// <returns>The same agent, so calls can be chained.</returns>
    public StandardAgent LocalJudge(Func<string, string, ValueTask<string>> evaluate) =>
        Set(() => this.localJudgeEvaluate = evaluate);

    /// <summary>
    /// Turns on the Gate: an opt-in guardian that screens each prompt before the brain sees
    /// it and can refuse. A bare agent runs no gate (SPEC.md §8.1 leaves it pass-through in the
    /// Core profile). It may reuse the brain's endpoint or point at a different model; either
    /// way the Gate is never the brain — it runs its own screening rubric (Data), honouring
    /// SPEC.md invariant 6.
    /// </summary>
    /// <param name="apiUrl">Base URL of the OpenAI-compatible endpoint for the Gate.</param>
    /// <param name="apiKey">API key for the endpoint (empty string if none is needed).</param>
    /// <param name="model">Model name to request for screening.</param>
    /// <param name="temperature">Sampling temperature; kept at 0.0 for a deterministic verdict.</param>
    /// <param name="maxTokens">Maximum tokens for the verdict. Defaults to 16.</param>
    /// <param name="timeoutSeconds">Per-request timeout in seconds. Defaults to 30.</param>
    /// <returns>The same agent, so calls can be chained.</returns>
    public StandardAgent Gate(
        string apiUrl,
        string apiKey,
        string model,
        double temperature = 0.0,
        int maxTokens = 16,
        int timeoutSeconds = 30) =>
        Set(() => this.gateSettings =
            new InferenceSettings(apiUrl, apiKey, model, temperature, maxTokens, timeoutSeconds));

    /// <summary>
    /// Turns on the Judge: an opt-in guardian that scores the brain's draft answer and sends it
    /// back for revision when the score is too low. Like the Gate it is off by default, may reuse
    /// the brain's endpoint or a different model, and never acts as the brain — it applies its own
    /// evaluation rubric (Data), honouring SPEC.md invariant 6.
    /// </summary>
    /// <param name="apiUrl">Base URL of the OpenAI-compatible endpoint for the Judge.</param>
    /// <param name="apiKey">API key for the endpoint (empty string if none is needed).</param>
    /// <param name="model">Model name to request for evaluation.</param>
    /// <param name="temperature">Sampling temperature; kept at 0.0 for a deterministic score.</param>
    /// <param name="maxTokens">Maximum tokens for the score. Defaults to 16.</param>
    /// <param name="timeoutSeconds">Per-request timeout in seconds. Defaults to 30.</param>
    /// <returns>The same agent, so calls can be chained.</returns>
    public StandardAgent Judge(
        string apiUrl,
        string apiKey,
        string model,
        double temperature = 0.0,
        int maxTokens = 16,
        int timeoutSeconds = 30) =>
        Set(() => this.judgeSettings =
            new InferenceSettings(apiUrl, apiKey, model, temperature, maxTokens, timeoutSeconds));

    /// <summary>
    /// Turns on a <b>deterministic</b> Gate: a guardian backed by a rule, not a model. It refuses
    /// any prompt containing one of <paramref name="refusePatterns"/> (case-insensitive) and accepts
    /// everything else — compliance that cannot be a coin-flip. Rides the same
    /// <see cref="IClassifierBroker"/> seam as the model-backed <see cref="Gate"/>, so the loop and
    /// the Tri-Nature are unchanged; only the substrate is swapped. The patterns are Data.
    /// </summary>
    /// <param name="refusePatterns">Substrings that, if present in a prompt, cause a refusal.</param>
    /// <returns>The same agent, so calls can be chained.</returns>
    public StandardAgent RuleGate(params string[] refusePatterns) =>
        Set(() => this.classifierBroker = new RuleClassifierBroker(refusePatterns));

    /// <summary>
    /// Turns on a <b>deterministic</b> Judge: a guardian backed by a rule, not a model. It passes a
    /// draft answer only when the answer contains every one of <paramref name="requiredPatterns"/>
    /// (case-insensitive), and otherwise rejects it — naming the first missing item as the revise-out
    /// reason. Rides the same <see cref="IVerifierBroker"/> seam as the model-backed <see cref="Judge"/>.
    /// The patterns are Data.
    /// </summary>
    /// <param name="requiredPatterns">Substrings the answer must contain to pass review.</param>
    /// <returns>The same agent, so calls can be chained.</returns>
    public StandardAgent RuleJudge(params string[] requiredPatterns) =>
        Set(() => this.verifierBroker = new RuleVerifierBroker(requiredPatterns));

    /// <summary>
    /// Gives the agent a memory file it reads on recall and writes to through the built-in
    /// <c>remember</c> tool, so facts survive across turns and runs.
    /// </summary>
    /// <param name="path">Path to the memory file (created if it does not exist).</param>
    /// <returns>The same agent, so calls can be chained.</returns>
    public StandardAgent Memory(string path) =>
        Set(() => this.memoryPath = path);

    /// <summary>
    /// Gives the agent a knowledge base — a folder of reference files searched each turn, with the
    /// most relevant matches seeded into the turn's observations for the brain to draw on.
    /// </summary>
    /// <param name="path">Folder holding the knowledge files.</param>
    /// <param name="pattern">Glob for which files to search. Defaults to <c>*.md</c>.</param>
    /// <param name="maxResults">Maximum matches fed in per turn. Defaults to 3.</param>
    /// <returns>The same agent, so calls can be chained.</returns>
    public StandardAgent Knowledge(string path, string pattern = "*.md", int maxResults = 3) =>
    Set(() =>
    {
        this.knowledgePath = path;
        this.knowledgePattern = pattern;
        this.knowledgeMaxResults = maxResults;
    });

    /// <summary>
    /// Connects an external Model Context Protocol (MCP) server, exposing its tools to the agent
    /// alongside any local <see cref="Tool(ITool)"/> registrations.
    /// </summary>
    /// <param name="endpointUrl">Base URL of the MCP server.</param>
    /// <param name="relativeUrl">Relative path appended to the base URL. Defaults to empty.</param>
    /// <param name="timeoutSeconds">Per-call timeout in seconds. Defaults to 30.</param>
    /// <returns>The same agent, so calls can be chained.</returns>
    public StandardAgent Mcp(string endpointUrl, string relativeUrl = "", int timeoutSeconds = 30) =>
    Set(() =>
    {
        this.mcpEndpointUrl = endpointUrl;
        this.mcpRelativeUrl = relativeUrl;
        this.mcpTimeoutSeconds = timeoutSeconds;
    });

    /// <summary>
    /// Registers one tool the agent may call. It is only advertised to the brain when it carries a
    /// description and a skill contains the <c>{{tools}}</c> marker (SPEC.md §6.1); otherwise it
    /// stays available but unlisted.
    /// </summary>
    /// <param name="tool">The tool to register.</param>
    /// <returns>The same agent, so calls can be chained.</returns>
    public StandardAgent Tool(ITool tool) =>
        Set(() => this.tools.Add(tool));

    /// <summary>
    /// Registers several tools at once — the batch equivalent of calling <see cref="Tool(ITool)"/>
    /// for each.
    /// </summary>
    /// <param name="tools">The tools to register.</param>
    /// <returns>The same agent, so calls can be chained.</returns>
    public StandardAgent Tools(IEnumerable<ITool> tools) =>
        Set(() => this.tools.AddRange(tools));

    /// <summary>
    /// Writes a step-by-step trace of the agent's run to a log file, organised as
    /// <c>Turn → Step → Process</c> (the Coordination → Orchestration → Foundation tiers).
    /// </summary>
    /// <param name="path">Path to the log file (created if it does not exist).</param>
    /// <param name="verbosity">
    /// How deep the trace goes: <see cref="TraceVerbosity.Summary"/> (Turn outcomes only),
    /// <see cref="TraceVerbosity.Natures"/> (the three natures per Turn), or
    /// <see cref="TraceVerbosity.Full"/> (every Process, the default).
    /// </param>
    /// <returns>The same agent, so calls can be chained.</returns>
    public StandardAgent LogTo(string path, TraceVerbosity verbosity = TraceVerbosity.Full) =>
    Set(() =>
    {
        this.logPath = path;
        this.traceVerbosity = verbosity;
    });

    /// <summary>
    /// Writes a structured, machine-readable audit log — one JSON object per trace event
    /// (turn, step, process, outcome, error) — to <paramref name="path"/>, alongside any
    /// human-readable <see cref="LogTo"/> trace. Always full detail, for ingestion into a SIEM
    /// or telemetry pipeline.
    /// </summary>
    /// <param name="path">Path to the JSON-lines audit file (created if it does not exist).</param>
    /// <returns>The same agent, so calls can be chained.</returns>
    public StandardAgent Audit(string path) =>
        Set(() => this.auditPath = path);

    /// <summary>
    /// Turns on <b>PII redaction at the brain boundary</b>: before any prompt reaches the brain,
    /// emails, SSNs, credit-card numbers and phone numbers are swapped for opaque <c>{{LABEL_N}}</c>
    /// tokens, and the brain's reply is rehydrated so the caller gets the real values back. The brain
    /// (and any remote host serving it) never sees the data in the clear. Off by default. The default
    /// rule set is Data — see <see cref="RedactionRules.Default"/>.
    /// </summary>
    /// <returns>The same agent, so calls can be chained.</returns>
    public StandardAgent Redact() =>
        Set(() => this.redactionRules = RedactionRules.Default);

    /// <summary>
    /// Restricts the agent to a <b>least-privilege</b> set of tools: the brain may still propose any
    /// tool, but only those named here are allowed to run — anything else is denied at the Direction
    /// perimeter before it executes, fed back so the agent can choose a permitted path. Off by default
    /// (no restriction). The allow-list is Data. Matching is case-insensitive.
    /// </summary>
    /// <param name="toolNames">The only tool names this agent is permitted to run.</param>
    /// <returns>The same agent, so calls can be chained.</returns>
    public StandardAgent AllowTools(params string[] toolNames) =>
        Set(() => this.allowedTools = toolNames);

    /// <summary>
    /// Caps how many Recall→Think→Act turns a single prompt may take before the agent stops —
    /// the shared budget across tool calls and Judge revisions. Defaults to 7. A value below 1
    /// is treated as 1.
    /// </summary>
    /// <param name="turns">Maximum turns per prompt.</param>
    /// <returns>The same agent, so calls can be chained.</returns>
    public StandardAgent MaxTurns(int turns) =>
        Set(() => this.maxTurns = turns < 1 ? 1 : turns);

    /// <summary>
    /// Swaps in a custom skill broker, replacing the default file-backed one. For advanced hosts
    /// that source skills from somewhere other than a folder.
    /// </summary>
    /// <param name="broker">The skill broker to use.</param>
    /// <returns>The same agent, so calls can be chained.</returns>
    public StandardAgent UseSkills(ISkillBroker broker) =>
        Set(() => this.skillBroker = broker);

    /// <summary>
    /// Swaps in a custom generator (brain) broker — the extension point for a runtime that streams
    /// natively, an alternative to <see cref="Brain"/> or <see cref="LocalBrain"/>.
    /// </summary>
    /// <param name="broker">The generator broker to use.</param>
    /// <returns>The same agent, so calls can be chained.</returns>
    public StandardAgent UseGenerator(IGeneratorBroker broker) =>
        Set(() => this.generatorBroker = broker);

    /// <summary>
    /// Swaps in a custom memory broker, replacing the default file-backed one set up by
    /// <see cref="Memory"/>.
    /// </summary>
    /// <param name="broker">The memory broker to use.</param>
    /// <returns>The same agent, so calls can be chained.</returns>
    public StandardAgent UseMemory(IMemoryBroker broker) =>
        Set(() => this.memoryBroker = broker);

    /// <summary>
    /// Swaps in a custom knowledge broker, replacing the default file-backed one set up by
    /// <see cref="Knowledge"/>.
    /// </summary>
    /// <param name="broker">The knowledge broker to use.</param>
    /// <returns>The same agent, so calls can be chained.</returns>
    public StandardAgent UseKnowledge(IKnowledgeBroker broker) =>
        Set(() => this.knowledgeBroker = broker);

    /// <summary>
    /// Swaps in a custom classifier broker to back the Gate, replacing the endpoint-backed one set
    /// up by <see cref="Gate"/>.
    /// </summary>
    /// <param name="broker">The classifier broker to use.</param>
    /// <returns>The same agent, so calls can be chained.</returns>
    public StandardAgent UseGate(IClassifierBroker broker) =>
        Set(() => this.classifierBroker = broker);

    /// <summary>
    /// Swaps in a custom verifier broker to back the Judge, replacing the endpoint-backed one set
    /// up by <see cref="Judge"/>.
    /// </summary>
    /// <param name="broker">The verifier broker to use.</param>
    /// <returns>The same agent, so calls can be chained.</returns>
    public StandardAgent UseJudge(IVerifierBroker broker) =>
        Set(() => this.verifierBroker = broker);

    /// <summary>
    /// Swaps in a custom MCP broker, replacing the HTTP-backed one set up by <see cref="Mcp"/>.
    /// </summary>
    /// <param name="broker">The MCP broker to use.</param>
    /// <returns>The same agent, so calls can be chained.</returns>
    public StandardAgent UseMcp(IMcpBroker broker) =>
        Set(() => this.mcpBroker = broker);

    /// <summary>
    /// Swaps in a custom logging broker for the agent's internal diagnostic logging.
    /// </summary>
    /// <param name="broker">The logging broker to use.</param>
    /// <returns>The same agent, so calls can be chained.</returns>
    public StandardAgent UseLogging(ILoggingBroker broker) =>
        Set(() => this.loggingBroker = broker);

    /// <summary>
    /// Runs the agent on a prompt to completion and returns the final answer. The first call
    /// composes the configured pieces (brain, skills, tools, guardians, memory, knowledge); later
    /// calls reuse that composition unless a builder method changed the configuration.
    /// </summary>
    /// <param name="prompt">The user's prompt.</param>
    /// <returns>The agent's final answer.</returns>
    // async, so a composition failure surfaces on await rather than being thrown
    // synchronously out of a method whose signature promises a ValueTask. A caller
    // doing `var task = agent.ProcessPromptAsync(p); ... await task;` would otherwise
    // be hit at the assignment, nowhere near the await they were guarding.
    public async ValueTask<string> ProcessPromptAsync(string prompt)
    {
        this.agent ??= Compose();

        return await this.agent.ProcessPromptAsync(prompt);
    }

    /// <summary>
    /// Runs the agent on a prompt and streams its progress as it happens — status updates, the
    /// brain's thinking, and response text arrive as <see cref="AgentStreamEvent"/> values rather
    /// than waiting for the final answer. Use this to surface a live view of the agent's work.
    /// </summary>
    /// <param name="prompt">The user's prompt.</param>
    /// <param name="cancellationToken">Token to stop streaming early.</param>
    /// <returns>An async stream of events describing the agent's run.</returns>
    // async iterator, so a composition failure surfaces when the caller starts
    // enumerating rather than at the call site — mirroring ProcessPromptAsync.
    public async IAsyncEnumerable<AgentStreamEvent> StreamPromptAsync(
        string prompt,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        this.agent ??= Compose();

        IAsyncEnumerable<AgentStreamEvent> events =
            this.agent.ProcessPromptStreamAsync(prompt, cancellationToken);

        await foreach (AgentStreamEvent streamEvent in events.WithCancellation(cancellationToken))
        {
            yield return streamEvent;
        }
    }

    // Every builder method drops the cached composition, so configuration set after a
    // prompt still takes effect. Returning `this` without doing that would silently
    // ignore the change.
    private StandardAgent Set(Action configure)
    {
        configure();
        this.agent = null;

        return this;
    }

    // Reads an optional prompt file (constitution, consumption) resolved against the build
    // output like skills. A missing file yields empty rather than an error, so a stale path
    // degrades to the built-in guardian policy instead of bricking composition.
    private static string ReadOptionalFile(IFileBroker file, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        string fullPath = Path.Combine(AppContext.BaseDirectory, path);

        return file.FileExists(fullPath)
            ? file.ReadFile(fullPath)
            : string.Empty;
    }

    // Assembles a guardian rubric from its parts in order, skipping any that are empty:
    // constitution first (the law), then the policy (what to screen or score), then the
    // contract (the output protocol the broker parses). The contract stays last so it is
    // never displaced, whatever the policy above it.
    private static string ComposeGuardianRubric(params string[] parts) =>
        string.Join("\n\n", parts.Where(part => string.IsNullOrWhiteSpace(part) is false));

    private IAgentCoordinationService Compose()
    {
        ValidateComposition();

        InferenceSettings? brain = this.brainSettings;

        IFileBroker file = new FileBroker();

        ILoggingBroker logging =
            this.loggingBroker ?? new LoggingBroker(
                new NullLogger<LoggingBroker>(),
                new TimeBroker(),
                this.traceVerbosity,
                string.IsNullOrEmpty(this.logPath) ? null : this.logPath,
                string.IsNullOrEmpty(this.auditPath) ? null : this.auditPath);

        IGeneratorBroker generator =
            this.generatorBroker ?? new GeneratorBroker(
                brain!.ApiUrl, brain.ApiKey, brain.Model,
                brain.Temperature, brain.MaxTokens, brain.TimeoutSeconds);

        IMemoryService memoryService = this.memoryBroker is null
            ? new MemoryService(file, Path.GetFullPath(this.memoryPath), logging)
            : new MemoryService(this.memoryBroker, logging);

        List<ITool> allTools = [.. this.tools, new RememberTool(memoryService)];

        string constitution = ReadOptionalFile(file, this.constitutionPath);
        string consumption = ReadOptionalFile(file, this.consumptionPath);

        string gatePolicy = string.IsNullOrWhiteSpace(consumption)
            ? GuardianPrompts.GatePolicy
            : consumption;

        string judgePolicy = string.IsNullOrWhiteSpace(consumption)
            ? GuardianPrompts.JudgePolicy
            : consumption;

        string gateRubric = ComposeGuardianRubric(
            constitution, gatePolicy, GuardianPrompts.GateContract);

        string judgeRubric = ComposeGuardianRubric(
            constitution, judgePolicy, GuardianPrompts.JudgeContract);

        IClassifierBroker classifier =
            this.classifierBroker
                ?? (this.localGateScreen is not null
                    ? new FunctionClassifierBroker(this.localGateScreen, gateRubric)
                    : this.gateSettings is null
                        ? new NotConfiguredClassifierBroker()
                        : new ClassifierBroker(
                            this.gateSettings.ApiUrl, this.gateSettings.ApiKey, this.gateSettings.Model,
                            this.gateSettings.Temperature, this.gateSettings.MaxTokens,
                            this.gateSettings.TimeoutSeconds, gateRubric));

        IVerifierBroker verifier =
            this.verifierBroker
                ?? (this.localJudgeEvaluate is not null
                    ? new FunctionVerifierBroker(this.localJudgeEvaluate, judgeRubric)
                    : this.judgeSettings is null
                        ? new NotConfiguredVerifierBroker()
                        : new VerifierBroker(
                            this.judgeSettings.ApiUrl, this.judgeSettings.ApiKey, this.judgeSettings.Model,
                            this.judgeSettings.Temperature, this.judgeSettings.MaxTokens,
                            this.judgeSettings.TimeoutSeconds, judgeRubric));

        IToolBroker toolBroker = new ToolBroker(allTools);

        IMcpBroker mcp =
            this.mcpBroker ?? (string.IsNullOrWhiteSpace(this.mcpEndpointUrl)
                ? new NotConfiguredMcpBroker()
                : new McpBroker(
                    this.mcpEndpointUrl, this.mcpRelativeUrl, this.mcpTimeoutSeconds));


        ISkillService skillService = this.skillBroker is null
            ? new SkillService(
                new FileSkillBroker(Path.Combine(AppContext.BaseDirectory, this.skillsPath)),
                logging)
            : new SkillService(this.skillBroker, logging);

        IKnowledgeService knowledgeService = this.knowledgeBroker is null
            ? new KnowledgeService(
                file,
                Path.GetFullPath(this.knowledgePath),
                this.knowledgePattern,
                this.knowledgeMaxResults,
                logging)
            : new KnowledgeService(this.knowledgeBroker, logging);

        DataOrchestrationService data = new(
            skillService,
            memoryService,
            knowledgeService,
            RenderToolCatalog(allTools),
            logging);

        DecisionOrchestrationService decision = new(
            new GateService(classifier, logging),
            new BrainService(generator, logging, this.redactionRules),
            new JudgeService(verifier, logging),
            logging);

        DirectionOrchestrationService direction = new(
            new InternalToolService(toolBroker, logging),
            new ExternalToolService(mcp, logging),
            new ReturnService(logging),
            logging,
            this.allowedTools);

        return new AgentCoordinationService(data, decision, direction, logging, this.maxTurns);
    }

    // The catalog a "{{tools}}" marker in the agent's Data expands into. Only tools that
    // carry a description are listed — a description is the opt-in (SPEC 6.1); a tool with
    // none is callable but not advertised. Derived from the registered tools, so it never
    // drifts from what is actually there.
    private static string RenderToolCatalog(IEnumerable<ITool> tools)
    {
        IEnumerable<string> describedTools = tools
            .Where(tool => string.IsNullOrWhiteSpace(tool.Description) is false)
            .Select(tool => $"- {tool.Name} — {tool.Description} parameters: {tool.Parameters}");

        return string.Join("\n", describedTools);
    }

}
