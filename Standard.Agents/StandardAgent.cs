// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Microsoft.Extensions.Logging.Abstractions;
using Standard.Agents.Brokers.Data;
using Standard.Agents.Brokers.Decision;
using Standard.Agents.Brokers.Direction;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Services.Coordinations;
using Standard.Agents.Services.Foundations.Data;
using Standard.Agents.Services.Foundations.Decision;
using Standard.Agents.Services.Foundations.Direction;
using Standard.Agents.Services.Orchestrations.Data;
using Standard.Agents.Services.Orchestrations.Decision;
using Standard.Agents.Services.Orchestrations.Direction;
using Standard.Agents.Tools;

namespace Standard.Agents;

// The composition root. SPEC.md 9: "DI is OPTIONAL. A hand-wired composition root is
// fully conformant." Compose() IS that root — the whole 1-3-9 graph in one readable
// method, no container, nothing to chase. That is No Magic taken literally.
//
// Two ways to configure each nature:
//   Brain/Gate/Judge/Memory/... — build the default broker from primitives
//   Use*(broker)                — swap the broker entirely
//
// The second is what makes invariant 7.6 reachable: point Gate and Judge at a
// different model, or a deterministic rule, without touching the library.
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

    // Optional, and pointing it at a different model is the whole point. Left unset,
    // the Gate falls back to the Brain's endpoint — SPEC.md 9 permits collapsing the
    // three Decision brokers onto one endpoint, and Theory Ch.5 calls it "three
    // interfaces, collapsible substrate: at small scale one model wears all three
    // hats". For anything safety-critical, set this — invariant 7.6 says the guardian
    // must not be the brain.
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

        // Gate and Judge fall back to the Brain's endpoint — collapsible substrate.
        // These stay nullable: ValidateComposition has already proved that any of the
        // three that is still null has a swapped-in broker and will never be read.
        InferenceSettings? brain = this.brainSettings;
        InferenceSettings? gate = this.gateSettings ?? brain;
        InferenceSettings? judge = this.judgeSettings ?? brain;

        ISkillBroker skill =
            this.skillBroker ?? new SkillBroker(this.skillsPath);

        IGeneratorBroker generator =
            this.generatorBroker ?? new GeneratorBroker(
                brain!.ApiUrl, brain.ApiKey, brain.Model,
                brain.Temperature, brain.MaxTokens, brain.TimeoutSeconds);

        IMemoryBroker memory =
            this.memoryBroker ?? new MemoryBroker(this.memoryPath);

        IKnowledgeBroker knowledge =
            this.knowledgeBroker ?? new KnowledgeBroker(
                this.knowledgePath, this.knowledgePattern, this.knowledgeMaxResults);

        IClassifierBroker classifier =
            this.classifierBroker ?? new ClassifierBroker(
                gate!.ApiUrl, gate.ApiKey, gate.Model,
                gate.Temperature, gate.MaxTokens, gate.TimeoutSeconds);

        IVerifierBroker verifier =
            this.verifierBroker ?? new VerifierBroker(
                judge!.ApiUrl, judge.ApiKey, judge.Model,
                judge.Temperature, judge.MaxTokens, judge.TimeoutSeconds);

        IToolBroker toolBroker = new ToolBroker(this.tools);

        // No endpoint configured means a Core-profile agent, which SPEC.md 8.1 says
        // has no External. It still needs something on the far side of Act's unknown-
        // tool route, so it gets a broker that says so honestly (#81).
        IMcpBroker mcp =
            this.mcpBroker ?? (string.IsNullOrWhiteSpace(this.mcpEndpointUrl)
                ? new NotConfiguredMcpBroker()
                : new McpBroker(
                    this.mcpEndpointUrl, this.mcpRelativeUrl, this.mcpTimeoutSeconds));

        ILogBroker log = this.logBroker ?? new LogBroker(this.logPath);

        // Null by default: diagnostics are the host's to wire, and the exception
        // ladder still throws whether or not anyone is listening.
        ILoggingBroker logging =
            this.loggingBroker ?? new LoggingBroker(new NullLogger<LoggingBroker>());

        DataOrchestrationService data = new(
            new SkillService(skill, logging),
            new MemoryService(memory, logging),
            new KnowledgeService(knowledge, logging),
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

    private sealed record InferenceSettings(
        string ApiUrl,
        string ApiKey,
        string Model,
        double Temperature,
        int MaxTokens,
        int TimeoutSeconds);
}
