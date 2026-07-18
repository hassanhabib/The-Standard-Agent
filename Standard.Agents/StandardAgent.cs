// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using Standard.Agents.Brokers.Classifiers;
using Standard.Agents.Brokers.Generators;
using Standard.Agents.Brokers.Knowledges;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Brokers.Logs;
using Standard.Agents.Brokers.Mcps;
using Standard.Agents.Brokers.Memorys;
using Standard.Agents.Brokers.Skills;
using Standard.Agents.Brokers.Tools;
using Standard.Agents.Brokers.Verifiers;
using Standard.Agents.Models.Clients.Agents;
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
    private string logPath = "log.txt";
    private string memoryPath = "memory.txt";
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
    private IMcpBroker? mcpBroker;
    private ILogBroker? logBroker;
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
    public StandardAgent(string apiUrl, string apiKey, string model)
    {
    }

    public StandardAgent Skills(string path) =>
        Set(() => this.skillsPath = path);

    public StandardAgent Brain(
        string apiUrl,
        string apiKey,
        string model,
        double temperature = 0.7,
        int maxTokens = 1024,
        int timeoutSeconds = 120) =>
        Set(() => this.brainSettings =
            new InferenceSettings(apiUrl, apiKey, model, temperature, maxTokens, timeoutSeconds));

    // The local, in-process counterpart to Brain(apiUrl, ...): plug your own inference
    // with a delegate and the agent makes no API calls. Pick one — a local brain or an
    // external one. For a runtime that streams natively, implement IGeneratorBroker and
    // pass it to UseGenerator instead.
    public StandardAgent LocalBrain(Func<string, string, ValueTask<string>> generate) =>
        Set(() => this.generatorBroker = new FunctionGeneratorBroker(generate));

    // Guardians are opt-in. A bare agent (brain only) runs no gate — SPEC.md 8.1 says
    // the Core profile MAY leave Gate and Judge as pass-through. Calling this turns the
    // Gate on; it may share the Brain's endpoint (SPEC.md 9's collapsible substrate) or
    // point at a different model. Even collapsed onto one endpoint the Gate is never the
    // Brain: it runs its own screening rubric (Data, not the agent's system prompt),
    // honouring invariant 6.
    public StandardAgent Gate(
        string apiUrl,
        string apiKey,
        string model,
        double temperature = 0.0,
        int maxTokens = 16,
        int timeoutSeconds = 30) =>
        Set(() => this.gateSettings =
            new InferenceSettings(apiUrl, apiKey, model, temperature, maxTokens, timeoutSeconds));

    public StandardAgent Judge(
        string apiUrl,
        string apiKey,
        string model,
        double temperature = 0.0,
        int maxTokens = 16,
        int timeoutSeconds = 30) =>
        Set(() => this.judgeSettings =
            new InferenceSettings(apiUrl, apiKey, model, temperature, maxTokens, timeoutSeconds));

    public StandardAgent Memory(string path) =>
        Set(() => this.memoryPath = path);

    public StandardAgent Knowledge(string path, string pattern = "*.md", int maxResults = 3) =>
        Set(() =>
        {
            this.knowledgePath = path;
            this.knowledgePattern = pattern;
            this.knowledgeMaxResults = maxResults;
        });

    public StandardAgent Mcp(string endpointUrl, string relativeUrl = "", int timeoutSeconds = 30) =>
        Set(() =>
        {
            this.mcpEndpointUrl = endpointUrl;
            this.mcpRelativeUrl = relativeUrl;
            this.mcpTimeoutSeconds = timeoutSeconds;
        });

    public StandardAgent Tool(ITool tool) =>
        Set(() => this.tools.Add(tool));

    public StandardAgent Tools(IEnumerable<ITool> tools) =>
        Set(() => this.tools.AddRange(tools));

    public StandardAgent LogTo(string path) =>
        Set(() => this.logPath = path);

    public StandardAgent UseSkills(ISkillBroker broker) =>
        Set(() => this.skillBroker = broker);

    public StandardAgent UseGenerator(IGeneratorBroker broker) =>
        Set(() => this.generatorBroker = broker);

    public StandardAgent UseMemory(IMemoryBroker broker) =>
        Set(() => this.memoryBroker = broker);

    public StandardAgent UseKnowledge(IKnowledgeBroker broker) =>
        Set(() => this.knowledgeBroker = broker);

    public StandardAgent UseGate(IClassifierBroker broker) =>
        Set(() => this.classifierBroker = broker);

    public StandardAgent UseJudge(IVerifierBroker broker) =>
        Set(() => this.verifierBroker = broker);

    public StandardAgent UseMcp(IMcpBroker broker) =>
        Set(() => this.mcpBroker = broker);

    public StandardAgent UseLog(ILogBroker broker) =>
        Set(() => this.logBroker = broker);

    public StandardAgent UseLogging(ILoggingBroker broker) =>
        Set(() => this.loggingBroker = broker);

    // async, so a composition failure surfaces on await rather than being thrown
    // synchronously out of a method whose signature promises a ValueTask. A caller
    // doing `var task = agent.ProcessPromptAsync(p); ... await task;` would otherwise
    // be hit at the assignment, nowhere near the await they were guarding.
    public async ValueTask<string> ProcessPromptAsync(string prompt)
    {
        this.agent ??= Compose();

        return await this.agent.ProcessPromptAsync(prompt);
    }

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

    private IAgentCoordinationService Compose()
    {
        ValidateComposition();

        InferenceSettings? brain = this.brainSettings;

        ISkillBroker skill =
            this.skillBroker ?? new SkillBroker(this.skillsPath);

        IGeneratorBroker generator =
            this.generatorBroker ?? new GeneratorBroker(
                brain!.ApiUrl, brain.ApiKey, brain.Model,
                brain.Temperature, brain.MaxTokens, brain.TimeoutSeconds);

        IMemoryBroker memory =
            this.memoryBroker ?? new MemoryBroker(this.memoryPath);

        List<ITool> allTools = [.. this.tools, new RememberTool(memory)];

        IKnowledgeBroker knowledge =
            this.knowledgeBroker ?? new KnowledgeBroker(
                this.knowledgePath, this.knowledgePattern, this.knowledgeMaxResults);

        IClassifierBroker classifier =
            this.classifierBroker ?? (this.gateSettings is null
                ? new NotConfiguredClassifierBroker()
                : new ClassifierBroker(
                    this.gateSettings.ApiUrl, this.gateSettings.ApiKey, this.gateSettings.Model,
                    this.gateSettings.Temperature, this.gateSettings.MaxTokens,
                    this.gateSettings.TimeoutSeconds, GuardianPrompts.Gate));

        IVerifierBroker verifier =
            this.verifierBroker ?? (this.judgeSettings is null
                ? new NotConfiguredVerifierBroker()
                : new VerifierBroker(
                    this.judgeSettings.ApiUrl, this.judgeSettings.ApiKey, this.judgeSettings.Model,
                    this.judgeSettings.Temperature, this.judgeSettings.MaxTokens,
                    this.judgeSettings.TimeoutSeconds, GuardianPrompts.Judge));

        IToolBroker toolBroker = new ToolBroker(allTools);

        IMcpBroker mcp =
            this.mcpBroker ?? (string.IsNullOrWhiteSpace(this.mcpEndpointUrl)
                ? new NotConfiguredMcpBroker()
                : new McpBroker(
                    this.mcpEndpointUrl, this.mcpRelativeUrl, this.mcpTimeoutSeconds));

        ILogBroker log = this.logBroker ?? new LogBroker(this.logPath);

        ILoggingBroker logging =
            this.loggingBroker ?? new LoggingBroker(new NullLogger<LoggingBroker>());

        DataOrchestrationService data = new(
            new SkillService(skill, logging),
            new MemoryService(memory, logging),
            new KnowledgeService(knowledge, logging),
            RenderToolCatalog(allTools),
            logging);

        DecisionOrchestrationService decision = new(
            new GateService(classifier, logging),
            new BrainService(generator, logging),
            new JudgeService(verifier, logging),
            logging);

        DirectionOrchestrationService direction = new(
            new InternalToolService(toolBroker, logging),
            new ExternalToolService(mcp, logging),
            new ReturnService(logging),
            logging);

        return new AgentCoordinationService(data, decision, direction, log, logging);
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
