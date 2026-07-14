using Microsoft.Extensions.Configuration;
using Standard.Agents;
using Standard.Agents.Brokers.Data;
using Standard.Agents.Brokers.Decision;
using Standard.Agents.Brokers.Direction;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Demo.Tools;
using Standard.Agents.Services.Coordinations;
using Standard.Agents.Services.Foundations.Data;
using Standard.Agents.Services.Foundations.Decision;
using Standard.Agents.Services.Foundations.Direction;
using Standard.Agents.Services.Orchestrations.Data;
using Standard.Agents.Services.Orchestrations.Decision;
using Standard.Agents.Services.Orchestrations.Direction;
using Standard.Agents.Tools;

IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

IConfigurationSection peer = configuration.GetSection("PeerLLMConfigurations");

// ---- the consumer's tools ----
ITool calculator = new CalculatorTool();

// ---- BROKERS (one resource each) ----
ISkillBroker skillBroker = new SkillBroker("Skills");
IMemoryBroker memoryBroker = new MemoryBroker();
IKnowledgeBroker knowledgeBroker = new KnowledgeBroker();
IClassifierBroker classifierBroker = new ClassifierBroker();
IGeneratorBroker generatorBroker = new GeneratorBroker(
    apiUrl: peer.GetValue<string>("ApiUrl")!,
    apiKey: peer.GetValue<string>("ApiKey")!,
    model: peer.GetValue<string>("Model")!,
    temperature: peer.GetValue<double>("Temperature"),
    maxTokens: peer.GetValue<int>("MaxTokens"));
IVerifierBroker verifierBroker = new VerifierBroker();
IToolBroker toolBroker = new ToolBroker([calculator]);
IMcpBroker mcpBroker = new McpBroker();
ILogBroker logBroker = new LogBroker("log.txt");

// ---- FOUNDATIONS (nine, one broker each) ----
ISkillService skillService = new SkillService(skillBroker);
IMemoryService memoryService = new MemoryService(memoryBroker);
IKnowledgeService knowledgeService = new KnowledgeService(knowledgeBroker);
IGateService gateService = new GateService(classifierBroker);
IBrainService brainService = new BrainService(generatorBroker);
IJudgeService judgeService = new JudgeService(verifierBroker);
IInternalToolService internalToolService = new InternalToolService(toolBroker);
IExternalToolService externalToolService = new ExternalToolService(mcpBroker);
IReturnService returnService = new ReturnService();

// ---- ORCHESTRATIONS (three) ----
IDataOrchestrationService dataOrchestration =
    new DataOrchestrationService(skillService, memoryService, knowledgeService);
IDecisionOrchestrationService decisionOrchestration =
    new DecisionOrchestrationService(gateService, brainService, judgeService);
IDirectionOrchestrationService directionOrchestration =
    new DirectionOrchestrationService(internalToolService, externalToolService, returnService);

// ---- COORDINATION (the Agent) ----
IAgent agent = new AgentCoordinationService(
    dataOrchestration, decisionOrchestration, directionOrchestration, logBroker);

string apiUrl = peer.GetValue<string>("ApiUrl") ?? "(unset)";

Console.WriteLine("Standard.Agents — Tri-Nature Agent");
Console.WriteLine($"Brain -> local PeerLLM at {apiUrl}");
Console.WriteLine($"Flow log -> {Path.GetFullPath("log.txt")}");
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
