// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Text.Json;
using Standard.Agents;
using Standard.Agents.Brokers.Approvals;
using Standard.Agents.Conformance;
using Standard.Agents.Models.Brokers.Audits;
using Standard.Agents.Models.Brokers.Generators.V1;

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
    // The order the run was unwound in, written by the stubs as they reverse themselves.
    List<string> compensationOrder = [];

    // One scripted native model for the whole vector, so its recorded conversations survive
    // across instances exactly as a real endpoint's would.
    ScriptedNativeGeneratorBroker? nativeGenerator = vector.NativeReplies is { Count: > 0 }
        ? new ScriptedNativeGeneratorBroker(vector.NativeReplies)
        : null;

    Queue<ApprovalDecision> approvalDecisions = new(
        (vector.ApprovalDecisions ?? []).Select(decision =>
            Enum.Parse<ApprovalDecision>(decision, ignoreCase: true)));

    Dictionary<string, StubTool> stubTools =
        (vector.Tools ?? []).ToDictionary(
            pair => pair.Key,
            pair => new StubTool(
                name: pair.Key,
                output: pair.Value,
                reversible: vector.CompensatingTools?.Contains(pair.Key) is true,
                compensationOrder: compensationOrder));

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
    int promptScreenings = 0;
    var scriptedGenerator = new ScriptedGeneratorBroker(vector.GeneratorReplies, vector.TransientFailures);

    var recordingGenerator = new RecordingGeneratorBroker(
        scriptedGenerator,
        input => { lock (auditLock) { modelInputs.Add(input); brainInputs.Add(input); } });

    // Anything on disk from a previous run is cleared once, here — not inside the builder, which
    // may be called again for the next prompt and would then erase the very session and ledger
    // that prompt is meant to resume from.
    foreach (string folder in
        (string[])["sessions", "ledger"])
    {
        string folderPath = Path.Combine(AppContext.BaseDirectory, $"{folder}-{vector.Name}");

        if (Directory.Exists(folderPath))
        {
            Directory.Delete(folderPath, recursive: true);
        }
    }

    // Each prompt may run through a brand-new instance, which is the closest this harness comes
    // to a different process: nothing but the files on disk survives between them. It is the only
    // way to certify that a session and an effect ledger outlive the agent that wrote them.
    async Task<StandardAgent> BuildAgentAsync()
    {
        StandardAgent agent = new StandardAgent()
            .UseSkills(new StubSkillBroker())
            .UseGenerator(recordingGenerator)
            .UseMemory(new StubMemoryBroker())
            .UseMcp(new NotConfiguredMcpBroker())
            .Tools(stubTools.Values)
            .OnGate((rubric, prompt) =>
            {
                gateRubric ??= rubric;

                lock (auditLock)
                {
                    modelInputs.Add(prompt);

                    if (prompt.Equals(vector.Prompt, StringComparison.Ordinal))
                    {
                        promptScreenings++;
                    }
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

        // A scripted authority, so a vector can hold an act on one run and permit it on the next
        // — the shape of every real approval, where the answer arrives after the agent stopped.
        // The queue is shared across instances, so the second process sees the second decision.
        if (vector.ApprovalDecisions is { Count: > 0 })
        {
            agent.OnApproval(effect =>
            {
                lock (auditLock)
                {
                    return ValueTask.FromResult(
                        approvalDecisions.Count > 0
                            ? approvalDecisions.Dequeue()
                            : ApprovalDecision.Pending);
                }
            });
        }

        if (vector.ScreenToolOutput)
        {
            agent.ScreenToolOutput();
        }

        if (vector.CompensateOnFailure)
        {
            agent.CompensateOnFailure();
        }

        if (nativeGenerator is not null)
        {
            agent.UseNativeBrain(nativeGenerator);
        }

        if (vector.Retries > 0)
        {
            agent.Resilience(vector.Retries);
        }

        if (vector.MaxTurns is int maxTurns)
        {
            agent.MaxTurns(maxTurns);
        }

        if (vector.BudgetMaxWallClockSeconds is double seconds)
        {
            agent.Budget(maxWallClock: TimeSpan.FromSeconds(seconds));
        }

        // A real session store, in a folder unique to this vector, so conversation is certified
        // against something that actually persists rather than an in-memory stand-in that could
        // never demonstrate resumption.
        if (vector.SessionId is not null)
        {
            agent.Sessions(Path.Combine(AppContext.BaseDirectory, $"sessions-{vector.Name}"));
        }

        if (vector.FallbackReply is string fallbackReply)
        {
            agent.Fallback(
                fallback: () => new ValueTask<string>(fallbackReply),
                failuresBeforeOpen: vector.FailuresBeforeOpen);
        }

        // A real knowledge folder, so the ranked retriever is certified against files rather than a
        // stub that could agree with any implementation. A vector without knowledge keeps the empty
        // stub, so nothing is retrieved and no vector is disturbed by this one existing.
        string? knowledgePath = null;

        if (vector.Knowledge is not { Count: > 0 })
        {
            agent.UseKnowledge(new StubKnowledgeBroker());
        }
        else
        {
            knowledgePath = Path.Combine(AppContext.BaseDirectory, $"knowledge-{vector.Name}");
            Directory.CreateDirectory(knowledgePath);

            foreach (KeyValuePair<string, string> document in vector.Knowledge)
            {
                await File.WriteAllTextAsync(
                    Path.Combine(knowledgePath, document.Key), document.Value);
            }

            agent.Knowledge(knowledgePath, maxResults: vector.KnowledgeMaxResults);
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

        // A folder-backed ledger, so run-once is certified against a claim that outlives the
        // instance that made it rather than one held in the instance's own memory.
        if (vector.DurableEffectLedger)
        {
            agent.EffectLedger(
                Path.Combine(AppContext.BaseDirectory, $"ledger-{vector.Name}"));
        }

        return agent;
    }

    StandardAgent agent = await BuildAgentAsync();

    IReadOnlyList<string> prompts = vector.Prompts is { Count: > 0 }
        ? vector.Prompts
        : [vector.Prompt];

    using var runCancellation = new CancellationTokenSource();

    if (vector.CancelBeforeStart)
    {
        await runCancellation.CancelAsync();
    }

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
            // A new instance per prompt where the vector asks for it: nothing but the files
            // survives between them, which is what makes the next prompt a different process
            // rather than the same one carrying on.
            StandardAgent instance = vector.NewInstancePerPrompt
                ? await BuildAgentAsync()
                : agent;

            result = await instance.ProcessPromptAsync(
                prompt, vector.SessionId ?? string.Empty, runCancellation.Token);
        }
    }

    return new VectorRun(result, stubTools, gateRubric, judgeRubric, judgeInput, modelInputs, brainInputs, promptScreenings, auditRecords, compensationOrder, nativeGenerator);
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

    // A tool result must come back as an answer to the call that asked for it (SPEC.md §6). Both
    // halves are checked: the assistant's request replayed with its id, and the tool's answer
    // naming that id. A framework that narrates the result as prose fails on the second.
    if (vector.Expect.ToolResultAnswersCall is string expectedCallId)
    {
        IReadOnlyList<ConversationMessage> lastConversation =
            run.NativeGenerator?.Conversations.LastOrDefault() ?? [];

        bool requestReplayed = lastConversation.Any(message =>
            message.Role is MessageRole.Assistant
                && message.ToolCalls.Any(call => call.Id == expectedCallId));

        bool answerNamesCall = lastConversation.Any(message =>
            message.Role is MessageRole.Tool && message.ToolCallId == expectedCallId);

        if (requestReplayed is false || answerNamesCall is false)
        {
            failure =
                $"the model was not handed call '{expectedCallId}' and its answer "
                    + $"(request replayed: {requestReplayed}, answer names call: {answerNamesCall})";

            return false;
        }
    }

    // The unwind runs in reverse, and only over what the run actually performed (SPEC.md §4.9).
    if (vector.Expect.CompensationOrder is List<string> expectedOrder
        && run.CompensationOrder.SequenceEqual(expectedOrder) is false)
    {
        failure =
            $"compensation ran [{string.Join(", ", run.CompensationOrder)}], "
                + $"expected [{string.Join(", ", expectedOrder)}]";

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

    // Retrieval is certified by what reached the Brain: the passage that answers the question
    // has to actually get there (SPEC.md §4.2).
    if (vector.Expect.BrainSees is string required
        && run.BrainInputs.Any(input => input.Contains(required, StringComparison.Ordinal)) is false)
    {
        failure = $"the Brain was never shown the retrieved text: {Show(required)}";

        return false;
    }

    if (vector.Expect.GateScreenedPromptTimes is int expectedScreenings
        && run.PromptScreenings != expectedScreenings)
    {
        failure =
            $"the Gate screened the prompt {run.PromptScreenings} time(s), "
                + $"expected {expectedScreenings}";

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
    int PromptScreenings,
    List<AuditRecord> AuditRecords,
    List<string> CompensationOrder,
    ScriptedNativeGeneratorBroker? NativeGenerator);
