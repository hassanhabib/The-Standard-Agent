// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Text.Json.Nodes;
using Standard.Agents.Models.Brokers.Agents;

namespace Standard.Agents.Brokers.Agents;

/// <summary>
/// The Local mode of the fleet seam: a folder where every <c>.json</c> document is an agent
/// (the same documents <c>StandardAgent.FromJson</c> composes), read in path order. A document's
/// <c>name</c> names the agent (the file's own name when absent) and its <c>description</c>
/// advertises it — no description, no advertisement, exactly like a tool.
/// </summary>
public sealed class FileAgentRegistryBroker : IAgentRegistryBroker
{
    private readonly string agentsPath;

    public FileAgentRegistryBroker(string agentsPath) =>
        this.agentsPath = agentsPath;

    public async ValueTask<IReadOnlyList<RegisteredAgent>> SelectAgentsAsync()
    {
        if (Directory.Exists(this.agentsPath) is false)
        {
            return [];
        }

        List<RegisteredAgent> agents = [];

        foreach (string documentPath in Directory
            .EnumerateFiles(this.agentsPath, "*.json")
            .OrderBy(path => path, StringComparer.Ordinal))
        {
            string document = await File.ReadAllTextAsync(documentPath);
            JsonNode? identity = JsonNode.Parse(document);

            agents.Add(new RegisteredAgent(
                Name: identity?["name"]?.GetValue<string>()
                    ?? Path.GetFileNameWithoutExtension(documentPath),
                Description: identity?["description"]?.GetValue<string>() ?? string.Empty,
                Agent: StandardAgent.FromJson(document)));
        }

        return agents;
    }
}
