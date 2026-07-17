// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using MinimalAgent;
using MinimalAgent.Brokers.Data;
using MinimalAgent.Brokers.Decision;
using MinimalAgent.Brokers.Direction;
using MinimalAgent.Services.Coordinations;
using MinimalAgent.Services.Foundations.Data;
using MinimalAgent.Services.Foundations.Decision;
using MinimalAgent.Services.Foundations.Direction;
using MinimalAgent.Services.Orchestrations.Data;
using MinimalAgent.Services.Orchestrations.Decision;
using MinimalAgent.Services.Orchestrations.Direction;
using MinimalAgent.Tools;

// The brain's endpoint comes from the environment, so no secret lives in the repo:
//   set AGENT_API_URL=http://localhost:3000/v1/
//   set AGENT_API_KEY=...
//   set AGENT_MODEL=...
string apiUrl = Environment.GetEnvironmentVariable("AGENT_API_URL") ?? "http://localhost:3000/v1/";
string apiKey = Environment.GetEnvironmentVariable("AGENT_API_KEY") ?? string.Empty;
string model = Environment.GetEnvironmentVariable("AGENT_MODEL") ?? "Qwen2.5-7B-Instruct-Q5_K_M.gguf";

// BROKERS — one thin liaison per resource. Eight of them; Return has none.
ISkillBroker skillBroker = new SkillBroker("Skills");
IMemoryBroker memoryBroker = new MemoryBroker();
IKnowledgeBroker knowledgeBroker = new KnowledgeBroker();
IClassifierBroker classifierBroker = new ClassifierBroker();
IGeneratorBroker generatorBroker = new GeneratorBroker(apiUrl, apiKey, model);
IVerifierBroker verifierBroker = new VerifierBroker();
IToolBroker toolBroker = new ToolBroker([new CalculatorTool()]);
IMcpBroker mcpBroker = new McpBroker();

// FOUNDATIONS — nine, each over exactly one broker.
ISkillService skillService = new SkillService(skillBroker);
IMemoryService memoryService = new MemoryService(memoryBroker);
IKnowledgeService knowledgeService = new KnowledgeService(knowledgeBroker);
IGateService gateService = new GateService(classifierBroker);
IBrainService brainService = new BrainService(generatorBroker);
IJudgeService judgeService = new JudgeService(verifierBroker);
IInternalToolService internalToolService = new InternalToolService(toolBroker);
IExternalToolService externalToolService = new ExternalToolService(mcpBroker);
IReturnService returnService = new ReturnService();

// ORCHESTRATIONS — three, one per nature. Each coordinates its three foundations.
IDataOrchestrationService dataOrchestration =
    new DataOrchestrationService(skillService, memoryService, knowledgeService);

IDecisionOrchestrationService decisionOrchestration =
    new DecisionOrchestrationService(gateService, brainService, judgeService);

IDirectionOrchestrationService directionOrchestration =
    new DirectionOrchestrationService(internalToolService, externalToolService, returnService);

// COORDINATION — one. This is the agent: Recall -> Think -> Act, until it is done.
IAgent agent = new AgentCoordinationService(
    dataOrchestration,
    decisionOrchestration,
    directionOrchestration);

Console.WriteLine("Minimal Tri-Nature Agent");
Console.WriteLine($"Brain -> {apiUrl} ({model})");
Console.WriteLine("Type a prompt (or 'exit'). Try: What is 89347 * 61293 + 4472?");
Console.WriteLine();

while (true)
{
    Console.Write("Prompt: ");

    string? prompt = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(prompt))
    {
        continue;
    }

    if (prompt.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    try
    {
        string answer = await agent.ProcessPromptAsync(prompt);

        Console.WriteLine($"Agent: {answer}");
    }
    catch (Exception exception)
    {
        Console.WriteLine($"Agent: [error] {exception.Message}");
    }

    Console.WriteLine();
}
