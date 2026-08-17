// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Text.Json;
using Standard.Agents;
using Standard.Agents.Conformance;
using Standard.Agents.Models.Brokers.Audits;

string? profileName = ReadProfileArgument(args);

// Drop the flag and the value it consumed, so an optional vectors path stays positional.
string[] positionalArgs = [.. PositionalArguments(args)];

string conformanceRoot = Path.Combine(FindRepositoryRoot(), "conformance");

string vectorsPath = positionalArgs.Length > 0
    ? positionalArgs[0]
    : Path.Combine(conformanceRoot, "vectors");

JsonSerializerOptions jsonOptions = new()
{
    PropertyNameCaseInsensitive = true
};

Console.WriteLine($"Conformance vectors: {vectorsPath}");

if (profileName is not null)
{
    Console.WriteLine($"Certifying profile: {profileName}");
}

Console.WriteLine();

int passed = 0;
int failed = 0;
HashSet<string> passedVectors = new(StringComparer.OrdinalIgnoreCase);

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

    bool guardianInputConformant =
        GuardianInputConformant(vector, run, actualResult, out string? guardianFailure);

    if (resultConformant
        && toolInputConformant
        && rubricConformant
        && auditConformant
        && guardianInputConformant)
    {
        passed++;
        passedVectors.Add(vector.Name);
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

        if (guardianInputConformant is false)
        {
            Console.WriteLine($"        guardian input: {guardianFailure}");
            Console.WriteLine($"        judge input: {Show(run.JudgeInput ?? "(judge never ran)")}");
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

if (profileName is null)
{
    return failed == 0 ? 0 : 1;
}

// Certifying a profile is a separate question from running the vectors: not "did everything
// we happen to have pass" but "is every requirement of this level actually evidenced". A
// requirement whose vector does not exist fails — otherwise a profile could be claimed by
// never writing its evidence, which is the failure mode profiles exist to prevent.
List<string> missing =
    ProfileRequirements(profileName, Path.Combine(conformanceRoot, "profiles"), jsonOptions)
        .Where(requirement => passedVectors.Contains(requirement) is false)
        .ToList();

Console.WriteLine();

if (missing.Count == 0)
{
    Console.WriteLine($"CERTIFIED  {profileName}");

    return failed == 0 ? 0 : 1;
}

Console.WriteLine($"NOT CERTIFIED  {profileName} — {missing.Count} requirement(s) unmet:");

foreach (string requirement in missing)
{
    Console.WriteLine($"  - {requirement}");
}

return 1;

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
    string? judgeInput = null;

    // The decision log is observed through its own Custom sink (SPEC.md §4.8), so the
    // certification watches the records the framework produces rather than any storage.
    // Concurrent vectors write from many runs at once, hence the lock.
    List<AuditRecord> auditRecords = [];
    object auditLock = new();

    // Everything any model was shown, so a vector can certify that a sensitive value reached
    // none of them (SPEC.md §4.6). The generator is wrapped rather than replaced, so the real
    // Brain path — redaction, rehydration, streaming buffers — is what gets certified.
    List<string> modelInputs = [];
    List<string> brainInputs = [];
    var scriptedGenerator = new ScriptedGeneratorBroker(vector.GeneratorReplies);

    var recordingGenerator = new RecordingGeneratorBroker(
        scriptedGenerator,
        input => { lock (auditLock) { modelInputs.Add(input); brainInputs.Add(input); } });

    StandardAgent agent = new StandardAgent()
        .UseSkills(new StubSkillBroker())
        .UseGenerator(recordingGenerator)
        .UseMemory(new StubMemoryBroker())
        .UseKnowledge(new StubKnowledgeBroker())
        .UseMcp(new NotConfiguredMcpBroker())
        .Tools(stubTools.Values)
        .OnGate((rubric, prompt) =>
        {
            gateRubric ??= rubric;

            lock (auditLock)
            {
                modelInputs.Add(prompt);
            }

            // Screening reuses the Gate, so the same scripted guardian answers for the prompt
            // and for tool output. A vector may script the two differently, which is how it
            // refuses an injected result while still letting the task through.
            bool isToolOutput = vector.ScreenToolOutput
                && prompt.Equals(vector.Prompt, StringComparison.Ordinal) is false;

            string verdict = isToolOutput && vector.GateVerdictOnToolOutput is not null
                ? vector.GateVerdictOnToolOutput
                : vector.GateVerdict ?? "allow";

            return new ValueTask<string>(verdict);
        })
        .OnJudge((rubric, candidate) =>
        {
            judgeRubric = rubric;
            judgeInput = candidate;

            lock (auditLock)
            {
                modelInputs.Add(candidate);
            }

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

    if (vector.Redact)
    {
        agent.Redact();
    }

    if (vector.RequireApproval is { Count: > 0 })
    {
        agent.RequireApproval([.. vector.RequireApproval]);
    }

    if (vector.ScreenToolOutput)
    {
        agent.ScreenToolOutput();
    }

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

    return new VectorRun(result, stubTools, gateRubric, judgeRubric, judgeInput, modelInputs, brainInputs, auditRecords);
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

// What a guardian was allowed to see, and what it was not allowed to become (SPEC.md §4.2, §7.6).
static bool GuardianInputConformant(
    Vector vector,
    VectorRun run,
    string actualResult,
    out string? failure)
{
    failure = null;

    if (vector.Expect.JudgeSawTask)
    {
        if (run.JudgeInput is null)
        {
            failure = "the judge never ran, so it cannot have seen the task";

            return false;
        }

        if (run.JudgeInput.Contains(vector.Prompt, StringComparison.Ordinal) is false)
        {
            failure = $"the judge never saw the task {Show(vector.Prompt)}";

            return false;
        }
    }

    if (vector.Expect.GuardianNeverAnswers
        && vector.GateVerdict is not null
        && actualResult.Contains(vector.GateVerdict, StringComparison.Ordinal))
    {
        failure = "the guardian's own text became the agent's answer";

        return false;
    }

    // Held is not performed, and proposing an act many times is still one act (SPEC.md §4.9).
    foreach (string toolName in vector.Expect.ToolNeverRan ?? [])
    {
        int actualRuns = run.Tools.TryGetValue(toolName, out StubTool? heldTool)
            ? heldTool.ReceivedInputs.Count
            : 0;

        if (actualRuns > 0)
        {
            failure = $"tool '{toolName}' ran {actualRuns} time(s); it should never have run";

            return false;
        }
    }

    foreach (KeyValuePair<string, int> expected in vector.Expect.ToolRunCount ?? [])
    {
        int actualRuns = run.Tools.TryGetValue(expected.Key, out StubTool? countedTool)
            ? countedTool.ReceivedInputs.Count
            : 0;

        if (actualRuns != expected.Value)
        {
            failure =
                $"tool '{expected.Key}' ran {actualRuns} time(s), expected {expected.Value}";

            return false;
        }
    }

    if (vector.Expect.BrainNeverSees is string withheld
        && run.BrainInputs.Any(input => input.Contains(withheld, StringComparison.Ordinal)))
    {
        failure = $"the Brain was shown text that should have been withheld: {Show(withheld)}";

        return false;
    }

    // Redaction is only satisfied if EVERY model call is clean. Checking the Brain alone is the
    // exact mistake this vector exists to catch (SPEC.md §4.6).
    if (vector.Expect.NoModelSees is string secret)
    {
        if (run.ModelInputs.Count == 0)
        {
            failure = "no model was called, so nothing was certified";

            return false;
        }

        int leaking = run.ModelInputs.Count(input =>
            input.Contains(secret, StringComparison.Ordinal));

        if (leaking > 0)
        {
            failure =
                $"{leaking} of {run.ModelInputs.Count} model call(s) saw {Show(secret)} in the clear";

            return false;
        }
    }

    return true;
}

static string? ReadProfileArgument(string[] args)
{
    int index = Array.FindIndex(args, argument =>
        argument.Equals("--profile", StringComparison.OrdinalIgnoreCase));

    return index >= 0 && index + 1 < args.Length
        ? args[index + 1]
        : null;
}

static IEnumerable<string> PositionalArguments(string[] args)
{
    for (int index = 0; index < args.Length; index++)
    {
        if (args[index].Equals("--profile", StringComparison.OrdinalIgnoreCase))
        {
            index++;

            continue;
        }

        yield return args[index];
    }
}

// A profile's requirements are its own plus everything it inherits, so certifying Reliable
// certifies Core too — a level is a floor, never a substitute.
static List<string> ProfileRequirements(
    string profileName,
    string profilesPath,
    JsonSerializerOptions jsonOptions)
{
    List<string> requirements = [];
    string? nextProfileName = profileName;

    while (nextProfileName is not null)
    {
        string profileFile =
            Path.Combine(profilesPath, $"{nextProfileName.ToLowerInvariant()}.json");

        if (File.Exists(profileFile) is false)
        {
            throw new FileNotFoundException(
                $"No such readiness profile: '{nextProfileName}'. Expected {profileFile}.");
        }

        Profile profile =
            JsonSerializer.Deserialize<Profile>(File.ReadAllText(profileFile), jsonOptions)!;

        requirements.AddRange(profile.Requires);
        nextProfileName = profile.Inherits;
    }

    return [.. requirements.Distinct(StringComparer.OrdinalIgnoreCase)];
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
    string? JudgeInput,
    List<string> ModelInputs,
    List<string> BrainInputs,
    List<AuditRecord> AuditRecords);
