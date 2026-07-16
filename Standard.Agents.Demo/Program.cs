// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Standard.Agents;
using Standard.Agents.Demo.Tools;

// Read configuration (the endpoint and credentials for the brain).
IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

IConfigurationSection peer = configuration.GetSection("PeerLLMConfigurations");

// The library wires every broker and service under the hood. The consumer supplies
// only what is theirs: where the skills live, the brain (LLM) config, the tools it
// offers, and where to write the flow log.
var agent = new StandardAgent()
    .Skills("Skills")
    .Brain(
        apiUrl: peer.GetValue<string>("ApiUrl")!,
        apiKey: peer.GetValue<string>("ApiKey")!,
        model: peer.GetValue<string>("Model")!,
        temperature: peer.GetValue<double>("Temperature"),
        maxTokens: peer.GetValue<int>("MaxTokens"))
    .Tool(new CalculatorTool())
    .LogTo("log.txt");

string apiUrl = peer.GetValue<string>("ApiUrl") ?? "(unset)";

Console.WriteLine("Standard.Agents — Tri-Nature Agent");
Console.WriteLine($"Brain -> {apiUrl}");
Console.WriteLine($"Flow log -> {Path.GetFullPath("log.txt")}");
Console.WriteLine("Type a prompt (or 'exit'). Try: What is 89347 * 61293 + 4472?");
Console.WriteLine();

// The consumer's whole interaction with the framework is IAgent.ProcessPromptAsync.
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
