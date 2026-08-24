// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

// The same agent definition as a service. Five lines on a laptop, the same five lines
// behind HTTP: composition here is configuration, never new concepts - the appliance
// guarantee holds at the exposure layer too.

using Standard.Agents;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// One agent, one singleton - one instance serving prompts concurrently is the intended
// shape (docs/support.md), and run state is per invocation by SPEC.md 4.4. Zero config
// must still run: without a Brain the host stands, heartbeats, and answers with what to
// configure rather than crashing at startup.
builder.Services.AddSingleton<IAgent>(provider =>
{
    IConfiguration configuration = provider.GetRequiredService<IConfiguration>();
    string? url = configuration["Agent:Url"];

    StandardAgent agent = string.IsNullOrWhiteSpace(url)
        ? new StandardAgent().OnBrain(async (_, _) =>
            "FINAL: No Brain is configured. Set Agent:Url, Agent:ApiKey and Agent:Model, "
                + "then restart the host.")
        : new StandardAgent(
            url,
            configuration["Agent:ApiKey"] ?? string.Empty,
            configuration["Agent:Model"] ?? string.Empty);

    string? skillsPath = configuration["Agent:Skills"];

    if (string.IsNullOrWhiteSpace(skillsPath) is false)
    {
        agent.Skills(skillsPath);
    }

    return agent;
});

var app = builder.Build();

app.MapControllers();

app.Run();
