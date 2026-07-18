// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Text.Json;
using Standard.Agents.Brokers.Memorys;

namespace Standard.Agents.Tools;

public sealed class RememberTool : ITool
{
    private readonly IMemoryBroker memoryBroker;

    public string Name => "remember";

    public string Description =>
        "Store a fact to remember across sessions. Use it when the user tells you "
            + "something worth keeping — their name, where they are, their preferences.";

    public string Parameters => "{ \"fact\": \"the fact to remember\" }";

    public RememberTool(IMemoryBroker memoryBroker) =>
        this.memoryBroker = memoryBroker;

    public async ValueTask<string> ExecuteAsync(string input)
    {
        string fact = ExtractFact(input);

        await Task.CompletedTask;

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
