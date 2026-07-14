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

public sealed class AgentBuilder
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

    public AgentBuilder Skills(string path)
    {
        this.skillsPath = path;
        return this;
    }

    public AgentBuilder Brain(
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
        return this;
    }

    public AgentBuilder Tool(ITool tool)
    {
        this.tools.Add(tool);
        return this;
    }

    public AgentBuilder Tools(IEnumerable<ITool> tools)
    {
        this.tools.AddRange(tools);
        return this;
    }

    public AgentBuilder LogTo(string path)
    {
        this.logPath = path;
        return this;
    }

    public AgentBuilder UseSkills(ISkillBroker broker)
    {
        this.skillBroker = broker;
        return this;
    }

    public AgentBuilder UseGenerator(IGeneratorBroker broker)
    {
        this.generatorBroker = broker;
        return this;
    }

    public AgentBuilder UseMemory(IMemoryBroker broker)
    {
        this.memoryBroker = broker;
        return this;
    }

    public AgentBuilder UseKnowledge(IKnowledgeBroker broker)
    {
        this.knowledgeBroker = broker;
        return this;
    }

    public AgentBuilder UseGate(IClassifierBroker broker)
    {
        this.classifierBroker = broker;
        return this;
    }

    public AgentBuilder UseJudge(IVerifierBroker broker)
    {
        this.verifierBroker = broker;
        return this;
    }

    public AgentBuilder UseMcp(IMcpBroker broker)
    {
        this.mcpBroker = broker;
        return this;
    }

    public AgentBuilder UseLog(ILogBroker broker)
    {
        this.logBroker = broker;
        return this;
    }

    public IAgent Build()
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
