// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Text.Json;
using Standard.Agents;
using Standard.Agents.Conformance;
using Standard.Agents.Tools;

// The reference conformance runner. It loads the language-neutral vectors, builds an
// agent with a SCRIPTED brain + STUB tools (via the library's AgentBuilder), runs each
// prompt, and asserts on the returned result. Other-language implementations mirror
// this harness.

string vectorsDir = args.Length > 0
    ? args[0]
    : Path.Combine(FindRepoRoot(), "conformance", "vectors");

JsonSerializerOptions jsonOptions = new() { PropertyNameCaseInsensitive = true };

Console.WriteLine($"Conformance vectors: {vectorsDir}");
Console.WriteLine();

int passed = 0;
int failed = 0;

foreach (string file in Directory.EnumerateFiles(vectorsDir, "*.json").OrderBy(path => path))
{
    Vector vector = JsonSerializer.Deserialize<Vector>(File.ReadAllText(file), jsonOptions)!;
    string actual = await RunAsync(vector);

    bool ok = vector.Expect.Result is not null
        ? actual == vector.Expect.Result
        : vector.Expect.ResultContains is not null
          && actual.Contains(vector.Expect.ResultContains, StringComparison.Ordinal);

    if (ok)
    {
        passed++;
        Console.WriteLine($"PASS  {vector.Name}");
    }
    else
    {
        failed++;
        Console.WriteLine($"FAIL  {vector.Name}");
        Console.WriteLine($"        expected: {vector.Expect.Result ?? "contains: " + vector.Expect.ResultContains}");
        Console.WriteLine($"        actual:   {actual}");
    }
}

Console.WriteLine();
Console.WriteLine($"{passed} passed, {failed} failed");

return failed == 0 ? 0 : 1;

// Build an agent with a scripted brain + stub tools, run one prompt. The AgentBuilder
// wires everything else (the library defaults) under the hood — so this also exercises
// the same composition a real consumer uses.
async Task<string> RunAsync(Vector vector)
{
    IEnumerable<ITool> tools =
        (vector.Tools ?? []).Select(pair => (ITool)new StubTool(pair.Key, pair.Value));

    IAgent agent = new AgentBuilder()
        .UseSkills(new StubSkillBroker())
        .UseGenerator(new ScriptedGeneratorBroker(vector.GeneratorReplies))
        .Tools(tools)
        .UseLog(new NullLogBroker())
        .Build();

    return await agent.ProcessPromptAsync(vector.Prompt);
}

static string FindRepoRoot()
{
    DirectoryInfo? directory = new(AppContext.BaseDirectory);

    while (directory is not null &&
           !Directory.Exists(Path.Combine(directory.FullName, "conformance")))
    {
        directory = directory.Parent;
    }

    return directory?.FullName ?? ".";
}
