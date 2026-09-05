// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Nodes;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Brokers.Mcps;
using Standard.Agents.Models.Brokers.Mcps;

namespace Standard.Agents.Services.Foundations.ExternalTools;

public partial class ExternalToolService : IExternalToolService
{
    // The one argument plain text travels as, for a tool that declared no schema of its own.
    private const string InputArgumentName = "input";

    private readonly IMcpBroker mcpBroker;
    private readonly ILoggingBroker loggingBroker;

    public ExternalToolService(
        IMcpBroker mcpBroker,
        ILoggingBroker loggingBroker)
    {
        this.mcpBroker = mcpBroker;
        this.loggingBroker = loggingBroker;
    }

    public ValueTask<string> CallAsync(string name, string input) =>
    TryCatch(async () =>
    {
        ValidateName(name);

        return await this.mcpBroker.CallAsync(name, ToArgumentsJson(input));
    });

    // The wire takes a JSON object of arguments. A native call already wrote one, and it goes
    // over exactly as written; the text protocol produces plain text, which travels as the one
    // argument a schema-less tool understands. Forcing every call into that one argument was
    // what left typed, multi-property tools uncallable (principal review 2026-09-04, F-03).
    private static string ToArgumentsJson(string input)
    {
        if (IsJsonObject(input))
        {
            return input;
        }

        return JsonSerializer.Serialize(
            new Dictionary<string, string> { [InputArgumentName] = input });
    }

    private static bool IsJsonObject(string input)
    {
        try
        {
            return JsonNode.Parse(input) is JsonObject;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public ValueTask<IReadOnlyList<McpTool>> RetrieveToolsAsync() =>
    TryCatch(async () => await this.mcpBroker.ListToolsAsync());
}
