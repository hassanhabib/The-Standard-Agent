// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Standard.Agents;
using Standard.Agents.Demo.Tools;

IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables()
    .Build();

IConfigurationSection settings = configuration.GetSection("PeerLLMConfigurations");

string apiKey = "9dS3/n8C58piIhaqemEnfhjlEZ5blXJQ+RxlMI+LRa0=";

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
    Console.Error.WriteLine($"{exception.GetType().Name}: {exception.Message}");

    if (exception.InnerException is not null)
    {
        Console.Error.WriteLine($"  caused by: {exception.InnerException.Message}");
    }

    return 1;
}
