// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Standard.Agents;
using Standard.Agents.Demo.Tools;

// The CLIENT exposer — "prompt in, answer out", the top row of architecture.svg.
//
// The Standard 3: an exposer is pure mapping with ONE service dependency and no
// business logic. That is the whole file: read config, hand the agent a prompt,
// print what comes back. Nothing here decides anything.
//
// Worth comparing to what this used to be: the previous demo hand-wired all nine
// foundations, three orchestrations and the coordination itself — about fifty lines
// of graph. That is the composition root's job, and StandardAgent is now the
// composition root (#36). An exposer that wires the system is not an exposer.

IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables()
    .Build();

IConfigurationSection settings = configuration.GetSection("PeerLLMConfigurations");

// The key is never in appsettings.json — The Standard 4.1.5. A local llama.cpp or
// Ollama endpoint needs none, hence the empty default rather than a hard failure.
string apiKey =
    Environment.GetEnvironmentVariable("STANDARD_AGENTS_APIKEY") ?? string.Empty;

var agent = new StandardAgent()
    .Skills("Skills")
    .Brain(
        apiUrl: settings.GetValue<string>("ApiUrl")!,
        apiKey: apiKey,
        model: settings.GetValue<string>("Model")!,
        temperature: settings.GetValue<double>("Temperature"),
        maxTokens: settings.GetValue<int>("MaxTokens"))
    .Tool(new CalculatorTool())
    .LogTo("log.txt");

string prompt = args.Length > 0
    ? string.Join(' ', args)
    : "What is 47 * 89, and what is the capital of France?";

Console.WriteLine($"Endpoint : {settings.GetValue<string>("ApiUrl")}");
Console.WriteLine($"Model    : {settings.GetValue<string>("Model")}");
Console.WriteLine($"Prompt   : {prompt}");
Console.WriteLine();

try
{
    string answer = await agent.ProcessPromptAsync(prompt);

    Console.WriteLine(answer);
    Console.WriteLine();
    Console.WriteLine("Turn-by-turn trace: log.txt");

    return 0;
}
catch (Exception exception)
{
    // The Standard 3.2: an exposer reports errors faithfully, in the protocol's own
    // form. For a console that means stderr and a non-zero exit — and the exception
    // type, because the ladder underneath already says which faculty failed and
    // whether waiting helps.
    Console.Error.WriteLine($"{exception.GetType().Name}: {exception.Message}");

    if (exception.InnerException is not null)
    {
        Console.Error.WriteLine($"  caused by: {exception.InnerException.Message}");
    }

    return 1;
}
