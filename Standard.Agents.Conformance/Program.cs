// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Text.Json;
using Standard.Agents;
using Standard.Agents.Conformance;
using Standard.Agents.Tools;

string vectorsPath = args.Length > 0
    ? args[0]
    : Path.Combine(FindRepositoryRoot(), "conformance", "vectors");

JsonSerializerOptions jsonOptions = new()
{
    PropertyNameCaseInsensitive = true
};

Console.WriteLine($"Conformance vectors: {vectorsPath}");
Console.WriteLine();

int passed = 0;
int failed = 0;

foreach (string vectorFile in
    Directory.EnumerateFiles(vectorsPath, "*.json").OrderBy(path => path, StringComparer.Ordinal))
{
    Vector vector =
        JsonSerializer.Deserialize<Vector>(await File.ReadAllTextAsync(vectorFile), jsonOptions)!;

    string actualResult = await RunVectorAsync(vector);

    bool isConformant = vector.Expect.Result is not null
        ? actualResult == vector.Expect.Result
        : vector.Expect.ResultContains is not null
            && actualResult.Contains(vector.Expect.ResultContains, StringComparison.Ordinal);

    if (isConformant)
    {
        passed++;
        Console.WriteLine($"PASS  {vector.Name}");
    }
    else
    {
        failed++;

        string expectation = vector.Expect.Result is not null
            ? vector.Expect.Result
            : $"contains: {vector.Expect.ResultContains}";

        Console.WriteLine($"FAIL  {vector.Name}");
        Console.WriteLine($"        expected: {Show(expectation)}");
        Console.WriteLine($"        actual:   {Show(actualResult)}");
    }
    }

Console.WriteLine();
Console.WriteLine($"{passed} passed, {failed} failed");

return failed == 0 ? 0 : 1;

async Task<string> RunVectorAsync(Vector vector)
{
    IEnumerable<ITool> tools =
        (vector.Tools ?? []).Select(pair => (ITool)new StubTool(name: pair.Key, output: pair.Value));

    IAgent agent = new StandardAgent()
        .UseSkills(new StubSkillBroker())
        .UseGenerator(new ScriptedGeneratorBroker(vector.GeneratorReplies))
        .UseMemory(new StubMemoryBroker())
        .UseKnowledge(new StubKnowledgeBroker())
        .UseGate(new AllowingClassifierBroker())
        .UseJudge(new ApprovingVerifierBroker())
        .UseMcp(new NotConfiguredMcpBroker())
        .UseLog(new NullLogBroker())
        .Tools(tools);

    return await agent.ProcessPromptAsync(vector.Prompt);
    }

static string Show(string value) =>
    value.Replace("\n", "\\n").Replace("\r", "\\r");

static string FindRepositoryRoot()
{
    DirectoryInfo? directory = new(AppContext.BaseDirectory);

    while (directory is not null
        && Directory.Exists(Path.Combine(directory.FullName, "conformance")) is false)
    {
        directory = directory.Parent;
    }

    return directory?.FullName
        ?? throw new DirectoryNotFoundException(
            "Could not find the repository root — no 'conformance' directory found "
                + "walking up from the executable.");
    }
