// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Text.Json;
using Standard.Agents;
using Standard.Agents.Conformance;
using Standard.Agents.Models.Brokers.Audits;

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

    VectorRun run = await RunVectorAsync(vector);
    string actualResult = run.Result;
    Dictionary<string, StubTool> stubTools = run.Tools;

    bool resultConformant = vector.Expect.Result is not null
        ? actualResult == vector.Expect.Result
        : vector.Expect.ResultContains is not null
            && actualResult.Contains(vector.Expect.ResultContains, StringComparison.Ordinal);

    bool toolInputConformant = vector.Expect.ToolInput is null
        || vector.Expect.ToolInput.All(expected =>
            stubTools.TryGetValue(expected.Key, out StubTool? tool)
                && tool.ReceivedInputs.Contains(expected.Value));

    bool rubricConformant =
        RubricConformant(vector.Expect, run.GateRubric, run.JudgeRubric, out string? rubricFailure);

    bool auditConformant =
        AuditConformant(vector, run.AuditRecords, out string? auditFailure);

    if (resultConformant && toolInputConformant && rubricConformant && auditConformant)
    {
        passed++;
        Console.WriteLine($"PASS  {vector.Name}");
    }
    else
    {
        failed++;
        Console.WriteLine($"FAIL  {vector.Name}");

        if (resultConformant is false)
        {
            string expectation = vector.Expect.Result is not null
                ? vector.Expect.Result
                : $"contains: {vector.Expect.ResultContains}";

            Console.WriteLine($"        expected result: {Show(expectation)}");
            Console.WriteLine($"        actual result:   {Show(actualResult)}");
        }

        if (toolInputConformant is false)
        {
            foreach (KeyValuePair<string, string> expected in vector.Expect.ToolInput!)
            {
                string actualInputs = stubTools.TryGetValue(expected.Key, out StubTool? tool)
                    ? string.Join(" | ", tool.ReceivedInputs.Select(Show))
                    : "(tool never called)";

                Console.WriteLine($"        tool '{expected.Key}' expected input: {Show(expected.Value)}");
                Console.WriteLine($"        tool '{expected.Key}' actual inputs:  {actualInputs}");
            }
        }

        if (rubricConformant is false)
        {
            Console.WriteLine($"        guardian rubric: {rubricFailure}");
            Console.WriteLine($"        gate rubric:  {Show(run.GateRubric ?? "(gate never ran)")}");
            Console.WriteLine($"        judge rubric: {Show(run.JudgeRubric ?? "(judge never ran)")}");
        }

        if (auditConformant is false)
        {
            Console.WriteLine($"        decision log: {auditFailure}");

            Console.WriteLine(
                $"        records: {run.AuditRecords.Count} "
                    + $"across {run.AuditRecords.Select(record => record.RunId).Distinct().Count()} run(s)");
        }
    }
}

Console.WriteLine();
Console.WriteLine($"{passed} passed, {failed} failed");

return failed == 0 ? 0 : 1;

async Task<VectorRun> RunVectorAsync(Vector vector)
{
    Dictionary<string, StubTool> stubTools =
        (vector.Tools ?? []).ToDictionary(
            pair => pair.Key,
            pair => new StubTool(name: pair.Key, output: pair.Value));

    // The guardians run through the real composition (OnGate / OnJudge), so each is handed
    // the rubric the framework composed — constitution, then policy (or the consumption skill),
    // then the framework-owned contract. The screen / evaluate delegates capture that rubric and
    // return a scripted verdict; the defaults ("allow" / "1.0") are inert, so a vector that sets
    // no guardian fields behaves exactly as an always-allowing gate and an always-approving judge.
    string? gateRubric = null;
    string? judgeRubric = null;

    // The decision log is observed through its own Custom sink (SPEC.md §4.8), so the
    // certification watches the records the framework produces rather than any storage.
    // Concurrent vectors write from many runs at once, hence the lock.
    List<AuditRecord> auditRecords = [];
    object auditLock = new();

    StandardAgent agent = new StandardAgent()
        .UseSkills(new StubSkillBroker())
        .UseGenerator(new ScriptedGeneratorBroker(vector.GeneratorReplies))
        .UseMemory(new StubMemoryBroker())
        .UseKnowledge(new StubKnowledgeBroker())
        .UseMcp(new NotConfiguredMcpBroker())
        .Tools(stubTools.Values)
        .OnGate((rubric, prompt) =>
        {
            gateRubric ??= rubric;

            return new ValueTask<string>(vector.GateVerdict ?? "allow");
        })
        .OnJudge((rubric, candidate) =>
        {
            judgeRubric = rubric;

            return new ValueTask<string>(vector.JudgeScore ?? "1.0");
        })
        .OnAudit(record =>
        {
            lock (auditLock)
            {
                auditRecords.Add(record);
            }

            return ValueTask.CompletedTask;
        });

    if (string.IsNullOrEmpty(vector.Constitution) is false)
    {
        string fileName = $"{vector.Name}.constitution.md";
        await File.WriteAllTextAsync(Path.Combine(AppContext.BaseDirectory, fileName), vector.Constitution);
        agent.Constitution(fileName);
    }

    if (string.IsNullOrEmpty(vector.Consumption) is false)
    {
        string fileName = $"{vector.Name}.consumption.md";
        await File.WriteAllTextAsync(Path.Combine(AppContext.BaseDirectory, fileName), vector.Consumption);
        agent.Consumption(fileName);
    }

    IReadOnlyList<string> prompts = vector.Prompts is { Count: > 0 }
        ? vector.Prompts
        : [vector.Prompt];

    string result;

    if (vector.Concurrent)
    {
        string[] results = await Task.WhenAll(
            prompts.Select(prompt => agent.ProcessPromptAsync(prompt).AsTask()));

        result = results[0];
    }
    else
    {
        result = string.Empty;

        foreach (string prompt in prompts)
        {
            result = await agent.ProcessPromptAsync(prompt);
        }
    }

    return new VectorRun(result, stubTools, gateRubric, judgeRubric, auditRecords);
}

// The decision log's guarantees, certified from the records themselves: one run per prompt,
// every prompt's evidence still present at the end, and record numbers that never repeat
// within a run (SPEC.md §4.7, §4.4).
static bool AuditConformant(Vector vector, List<AuditRecord> records, out string? failure)
{
    failure = null;

    Expectation expect = vector.Expect;

    if (expect.AuditRunCount is null
        && expect.AuditRetainsEveryPrompt is false
        && expect.AuditSequencesUniquePerRun is false)
    {
        return true;
    }

    IReadOnlyList<string> prompts = vector.Prompts is { Count: > 0 }
        ? vector.Prompts
        : [vector.Prompt];

    if (expect.AuditRunCount is int expectedRunCount)
    {
        int actualRunCount = records.Select(record => record.RunId).Distinct().Count();

        if (actualRunCount != expectedRunCount)
        {
            failure = $"expected {expectedRunCount} distinct run(s), found {actualRunCount}";

            return false;
        }
    }

    if (expect.AuditRetainsEveryPrompt)
    {
        foreach (string prompt in prompts)
        {
            bool retained = records.Any(record =>
                record.Message.Contains(prompt, StringComparison.Ordinal));

            if (retained is false)
            {
                failure = $"no record retained evidence of the prompt {Show(prompt)}";

                return false;
            }
        }
    }

    if (expect.AuditSequencesUniquePerRun)
    {
        foreach (IGrouping<string, AuditRecord> run in records.GroupBy(record => record.RunId))
        {
            int[] sequences = run.Select(record => record.Sequence).ToArray();

            if (sequences.Length != sequences.Distinct().Count())
            {
                failure = $"run '{run.Key}' repeated a record number — runs shared a counter";

                return false;
            }
        }
    }

    return true;
}

// A rubric guarantee is certified against BOTH guardians, so both rubrics must have been
// produced. A vector that asserts a rubric therefore has to drive the agent all the way to a
// judged final answer (gate allows, brain answers) — otherwise the judge never runs and there
// is nothing to certify against, which is reported as a failure rather than passing vacuously.
static bool RubricConformant(
    Expectation expect,
    string? gateRubric,
    string? judgeRubric,
    out string? failure)
{
    failure = null;

    if (expect.GuardianRubricContains is null && expect.GuardianRubricExcludes is null)
    {
        return true;
    }

    if (gateRubric is null || judgeRubric is null)
    {
        failure = "a guardian rubric was never produced "
            + $"(gate ran: {gateRubric is not null}, judge ran: {judgeRubric is not null})";

        return false;
    }

    foreach (string needle in expect.GuardianRubricContains ?? [])
    {
        if (gateRubric.Contains(needle, StringComparison.Ordinal) is false
            || judgeRubric.Contains(needle, StringComparison.Ordinal) is false)
        {
            failure = $"expected in both guardian rubrics: {Show(needle)}";

            return false;
        }
    }

    foreach (string needle in expect.GuardianRubricExcludes ?? [])
    {
        if (gateRubric.Contains(needle, StringComparison.Ordinal)
            || judgeRubric.Contains(needle, StringComparison.Ordinal))
        {
            failure = $"expected absent from both guardian rubrics: {Show(needle)}";

            return false;
        }
    }

    return true;
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

internal sealed record VectorRun(
    string Result,
    Dictionary<string, StubTool> Tools,
    string? GateRubric,
    string? JudgeRubric,
    List<AuditRecord> AuditRecords);
