// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Text.Json.Nodes;

string school = Environment.GetEnvironmentVariable("STUDENTS_SCHOOL") ?? "no school";
string term = args.Length > 0 ? args[0] : "no term";

while (Console.ReadLine() is string line)
{
    JsonNode message = JsonNode.Parse(line)!;

    if (message["id"] is null)
    {
        continue;
    }

    JsonNode result = message["method"]!.GetValue<string>() switch
    {
        "initialize" => JsonNode.Parse(
            """{"protocolVersion":"2025-06-18","capabilities":{"tools":{}},"serverInfo":{"name":"students","version":"1.0.0"}}""")!,

        "tools/list" => JsonNode.Parse(
            """{"tools":[{"name":"find_student","description":"Finds a student by their id.","inputSchema":{"type":"object","properties":{"id":{"type":"integer"}}}}]}""")!,

        _ => FindStudent(message["params"]?["arguments"])
    };

    var answer = new JsonObject
    {
        ["jsonrpc"] = "2.0",
        ["id"] = message["id"]!.DeepClone(),
        ["result"] = result
    };

    Console.WriteLine(answer.ToJsonString());
}

JsonNode FindStudent(JsonNode? arguments)
{
    string studentId = (arguments?["id"] ?? arguments?["input"])?.ToString() ?? "unknown";
    string text = $"student {studentId} is Hassan, at {school}, for the {term} term";

    return new JsonObject
    {
        ["content"] = new JsonArray(new JsonObject { ["type"] = "text", ["text"] = text })
    };
}
