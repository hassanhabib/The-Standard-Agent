// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

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

public sealed class StandardAgent : IAgent
{
    private readonly List<ITool> tools = [];

    private string skillsPath = "Skills";
    private string logPath = "log.txt";
    private string apiUrl = string.Empty;
    private string apiKey = string.Empty;
    private string model = string.Empty;
    private double temperature = 0.7;
    private int maxTokens = 1024;

    private ISkillBroker? skillBroker;
    private IGeneratorBroker? generatorBroker;
    private IMemoryBroker? memoryBroker;
    private IKnowledgeBroker? knowledgeBroker;
    private IClassifierBroker? classifierBroker;
    private IVerifierBroker? verifierBroker;
    private IMcpBroker? mcpBroker;
    private ILogBroker? logBroker;

    private IAgent? agent;

    public StandardAgent Skills(string path)
    {
        this.skillsPath = path;
        this.agent = null;
        return this;
    }

    public StandardAgent Brain(
        string apiUrl,
        string apiKey,
        string model,
        double temperature = 0.7,
        int maxTokens = 1024)
    {
        this.apiUrl = apiUrl;
        this.apiKey = apiKey;
        this.model = model;
        this.temperature = temperature;
        this.maxTokens = maxTokens;
        this.agent = null;
        return this;
    }

    public StandardAgent Tool(ITool tool)
    {
        this.tools.Add(tool);
        this.agent = null;
        return this;
    }

    public StandardAgent Tools(IEnumerable<ITool> tools)
    {
        this.tools.AddRange(tools);
        this.agent = null;
        return this;
    }

    public StandardAgent LogTo(string path)
    {
        this.logPath = path;
        this.agent = null;
        return this;
    }

    public StandardAgent UseSkills(ISkillBroker broker)
    {
        this.skillBroker = broker;
        this.agent = null;
        return this;
    }

    public StandardAgent UseGenerator(IGeneratorBroker broker)
    {
        this.generatorBroker = broker;
        this.agent = null;
        return this;
    }

    public StandardAgent UseMemory(IMemoryBroker broker)
    {
        this.memoryBroker = broker;
        this.agent = null;
        return this;
    }

    public StandardAgent UseKnowledge(IKnowledgeBroker broker)
    {
        this.knowledgeBroker = broker;
        this.agent = null;
        return this;
    }

    public StandardAgent UseGate(IClassifierBroker broker)
    {
        this.classifierBroker = broker;
        this.agent = null;
        return this;
    }

    public StandardAgent UseJudge(IVerifierBroker broker)
    {
        this.verifierBroker = broker;
        this.agent = null;
        return this;
    }

    public StandardAgent UseMcp(IMcpBroker broker)
    {
        this.mcpBroker = broker;
        this.agent = null;
        return this;
    }

    public StandardAgent UseLog(ILogBroker broker)
    {
        this.logBroker = broker;
        this.agent = null;
        return this;
    }

    public ValueTask<string> ProcessPromptAsync(string prompt)
    {
        this.agent ??= Compose();
        return this.agent.ProcessPromptAsync(prompt);
    }

    private IAgent Compose()
    {
        ISkillBroker skill = this.skillBroker ?? new SkillBroker(this.skillsPath);
        IGeneratorBroker generator = this.generatorBroker
            ?? new GeneratorBroker(this.apiUrl, this.apiKey, this.model, this.temperature, this.maxTokens);
        IMemoryBroker memory = this.memoryBroker ?? new MemoryBroker();
        IKnowledgeBroker knowledge = this.knowledgeBroker ?? new KnowledgeBroker();
        IClassifierBroker classifier = this.classifierBroker ?? new ClassifierBroker();
        IVerifierBroker verifier = this.verifierBroker ?? new VerifierBroker();
        IToolBroker toolBroker = new ToolBroker(this.tools);
        IMcpBroker mcp = this.mcpBroker ?? new McpBroker();
        ILogBroker log = this.logBroker ?? new LogBroker(this.logPath);

        DataOrchestrationService data = new(
            new SkillService(skill),
            new MemoryService(memory),
            new KnowledgeService(knowledge));

        DecisionOrchestrationService decision = new(
            new GateService(classifier),
            new BrainService(generator),
            new JudgeService(verifier));

        DirectionOrchestrationService direction = new(
            new InternalToolService(toolBroker),
            new ExternalToolService(mcp),
            new ReturnService());

        return new AgentCoordinationService(data, decision, direction, log);
    }
}
