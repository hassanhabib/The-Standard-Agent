// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Text.Json;
using Standard.Agents.Services.Foundations.Memorys;

namespace Standard.Agents.Tools;

public sealed class RememberTool : ITool
{
    // A delegate, not the memory service. A tool is reached through ToolBroker, underneath the
    // foundations, so a tool that KEEPS a service re-enters the tier stack from below it —
    // TierDisciplineTests.ShouldKeepFrameworkToolsOutOfTheTierStack watches this seam. The tool
    // holds the same shape a host-authored tool holds: a function it was handed.
    private readonly Func<string, ValueTask> remember;

    public string Name => "remember";

    public string Description =>
        "Store a fact to remember across sessions. Use it when the user tells you "
            + "something worth keeping — their name, where they are, their preferences.";

    // A JSON SCHEMA, not example JSON. The wire validates this strictly: a hosted provider
    // rejects the WHOLE request when any advertised tool's parameters is not a schema of type
    // object — which made every native run against a strict endpoint fail the moment memory
    // was composed in. Found by the first production consumer, not by any scripted test,
    // because scripted brokers accept anything.
    public string Parameters =>
        "{\"type\":\"object\",\"properties\":{\"fact\":{\"type\":\"string\","
            + "\"description\":\"the fact to remember\"}},\"required\":[\"fact\"]}";

    public RememberTool(Func<string, ValueTask> remember) =>
        this.remember = remember;

    // The converting alias, kept so nothing written against an earlier release breaks — the
    // same pattern as LocalBrain → OnBrain. It accepts the service and keeps only its routine.
    [Obsolete("Pass the remember routine itself — a tool holds a function, not a service. " +
        "This alias keeps working.")]
    public RememberTool(IMemoryService memoryService) :
        this(memoryService.RememberAsync)
    {
    }

    public async ValueTask<string> ExecuteAsync(string input)
    {
        string fact = ExtractFact(input);

        await this.remember(fact);

        return $"Remembered: {fact}";
    }

    private static string ExtractFact(string input)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(input);

            if (document.RootElement.ValueKind is JsonValueKind.Object
                && document.RootElement.TryGetProperty("fact", out JsonElement factElement)
                && factElement.ValueKind is JsonValueKind.String)
            {
                return factElement.GetString() ?? input;
            }
        }
        catch (JsonException)
        {
        }

        return input;
    }
}
