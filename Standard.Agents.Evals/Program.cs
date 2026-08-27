// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

// Conformance pins contracts; this pins QUALITY. Each golden case composes a real agent over
// a deterministic script and measures what the plan named: task completion, groundedness,
// retrieval precision and recall, tool selection, refusal correctness, and revision
// effectiveness. Every metric is computed only where the case supplies its golden data, a
// threshold that binds nothing is an error rather than silent coverage, and the whole run is
// attributable — framework version and golden-set hash on every report — because a passing
// score nobody can attribute is a score nobody can investigate.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Standard.Agents;
using Standard.Agents.Evals;
using Standard.Agents.Models.Clients.Agents;
using Standard.Agents.Models.Orchestrations.Agents;

string[] knownMetrics =
[
    "taskCompletion",
    "groundedness",
    "retrievalPrecision",
    "retrievalRecall",
    "toolSelection",
    "refusalCorrectness",
    "revisionEffectiveness"
];

string goldenPath = args.Length > 0
    ? args[0]
    : Path.Combine(FindRepositoryRoot(), "evals", "golden");

JsonSerializerOptions jsonOptions = new()
{
    PropertyNameCaseInsensitive = true
};

string frameworkVersion =
    typeof(StandardAgent).Assembly.GetName().Version?.ToString() ?? "unknown";

string goldenSetHash = HashGoldenSet(goldenPath);

Console.WriteLine($"Golden set: {goldenPath}");
Console.WriteLine($"Framework:  Standard.Agents {frameworkVersion}");
Console.WriteLine($"Set hash:   {goldenSetHash}");
Console.WriteLine();

int passed = 0;
int failed = 0;

foreach (string caseFile in
    Directory.EnumerateFiles(goldenPath, "*.json").OrderBy(path => path, StringComparer.Ordinal))
{
    EvalCase evalCase =
        JsonSerializer.Deserialize<EvalCase>(await File.ReadAllTextAsync(caseFile), jsonOptions)!;

    Dictionary<string, double> scores = await ScoreCaseAsync(evalCase);

    List<string> verdicts = [];
    bool caseFailed = false;

    foreach (KeyValuePair<string, double> threshold in evalCase.Thresholds)
    {
        if (knownMetrics.Contains(threshold.Key) is false)
        {
            caseFailed = true;
            verdicts.Add($"unknown metric '{threshold.Key}' — knowable: {string.Join(", ", knownMetrics)}");

            continue;
        }

        if (scores.TryGetValue(threshold.Key, out double score) is false)
        {
            // A threshold over a metric no prompt supplies golden data for would read as
            // coverage while measuring nothing — the vacuous-vector lesson, applied here.
            caseFailed = true;
            verdicts.Add($"{threshold.Key}: threshold {threshold.Value:0.00} binds nothing — no prompt carries its golden data");

            continue;
        }

        bool met = score >= threshold.Value;
        caseFailed |= met is false;

        verdicts.Add($"{threshold.Key} = {score:0.00} (threshold {threshold.Value:0.00}){(met ? "" : "  ← UNMET")}");
    }

    // A metric measured but not thresholded is reported, not judged — visible drift beats
    // silent drift.
    foreach (KeyValuePair<string, double> score in scores
        .Where(score => evalCase.Thresholds.ContainsKey(score.Key) is false))
    {
        verdicts.Add($"{score.Key} = {score.Value:0.00} (unthresholded)");
    }

    if (caseFailed)
    {
        failed++;
        Console.WriteLine($"FAIL  {evalCase.Name}");
    }
    else
    {
        passed++;
        Console.WriteLine($"PASS  {evalCase.Name}");
    }

    foreach (string verdict in verdicts)
    {
        Console.WriteLine($"        {verdict}");
    }
}

Console.WriteLine();
Console.WriteLine($"{passed} passed, {failed} failed  [Standard.Agents {frameworkVersion}, set {goldenSetHash}]");

return failed == 0 ? 0 : 1;

async Task<Dictionary<string, double>> ScoreCaseAsync(EvalCase evalCase)
{
    List<double> taskCompletion = [];
    List<double> groundedness = [];
    List<double> retrievalPrecision = [];
    List<double> retrievalRecall = [];
    List<double> toolSelection = [];
    List<double> refusalCorrectness = [];

    // The knowledge under retrieval, written to real files and served by the real ranked
    // lexical retrieval — the framework's retrieval is what is being measured.
    string? knowledgePath = null;

    if (evalCase.Knowledge is { Count: > 0 })
    {
        knowledgePath = Path.Combine(
            AppContext.BaseDirectory, $"knowledge-{evalCase.Name}");

        if (Directory.Exists(knowledgePath))
        {
            Directory.Delete(knowledgePath, recursive: true);
        }

        Directory.CreateDirectory(knowledgePath);

        foreach (KeyValuePair<string, string> passage in evalCase.Knowledge)
        {
            await File.WriteAllTextAsync(
                Path.Combine(knowledgePath, $"{passage.Key}.md"), passage.Value);
        }
    }

    // Judge scores are one queue across the case: a revision is one prompt judged twice.
    Queue<string> judgeScores = new(evalCase.JudgeScores ?? []);
    int judgeAsked = 0;
    AgentStatus finalStatus = AgentStatus.Failed;
    bool finalCarriedItsFacts = false;

    for (int promptIndex = 0; promptIndex < evalCase.Prompts.Count; promptIndex++)
    {
        EvalPrompt evalPrompt = evalCase.Prompts[promptIndex];
        List<string> brainInputs = [];

        Dictionary<string, RecordingTool> tools =
            (evalCase.Tools ?? []).ToDictionary(
                pair => pair.Key,
                pair => new RecordingTool(name: pair.Key, output: pair.Value));

        string gateVerdict =
            evalCase.GateVerdicts is not null && promptIndex < evalCase.GateVerdicts.Count
                ? evalCase.GateVerdicts[promptIndex]
                : "allow";

        StandardAgent agent = new StandardAgent()
            .UseSkills(new CaseSkillBroker(evalCase.Skill))
            .UseMemory(new CaseMemoryBroker(evalCase.Memories ?? []))
            .UseMcp(new NotConfiguredMcpBroker())
            .Tools(tools.Values)
            .UseGenerator(new RecordingGeneratorBroker(
                new ScriptedGeneratorBroker(evalCase.GeneratorReplies),
                brainInputs.Add))
            .OnGate(async (_, _) => gateVerdict)
            .OnJudge(async (_, _) =>
            {
                judgeAsked++;

                return judgeScores.Count > 0 ? judgeScores.Dequeue() : "1.0";
            });

        if (knowledgePath is not null)
        {
            agent.Knowledge(
                knowledgePath,
                maxResults: evalCase.KnowledgeMaxResults,
                minScore: evalCase.KnowledgeMinScore);
        }

        AgentOutcome outcome = await agent.RunAsync(evalPrompt.Prompt);
        finalStatus = outcome.Status;

        if (evalPrompt.AnswerMustContain is { Count: > 0 })
        {
            finalCarriedItsFacts = evalPrompt.AnswerMustContain
                .All(fact => outcome.Result.Contains(fact, StringComparison.Ordinal));

            taskCompletion.Add(finalCarriedItsFacts ? 1 : 0);
        }

        if (evalPrompt.MustCite is { Count: > 0 } || evalPrompt.MustNotClaim is { Count: > 0 })
        {
            bool citesWhatItRead = (evalPrompt.MustCite ?? []).All(citation =>
                outcome.Result.Contains(citation, StringComparison.Ordinal)
                    && brainInputs.Any(input =>
                        input.Contains(citation, StringComparison.Ordinal)));

            bool fabricatesNothing = (evalPrompt.MustNotClaim ?? []).All(claim =>
                outcome.Result.Contains(claim, StringComparison.Ordinal) is false);

            groundedness.Add(citesWhatItRead && fabricatesNothing ? 1 : 0);
        }

        if (evalPrompt.RelevantKnowledge is not null && evalCase.Knowledge is { Count: > 0 })
        {
            HashSet<string> retrieved =
                [.. evalCase.Knowledge
                    .Where(passage => brainInputs.Any(input =>
                        input.Contains(passage.Value, StringComparison.Ordinal)))
                    .Select(passage => passage.Key)];

            HashSet<string> relevant = [.. evalPrompt.RelevantKnowledge];
            int overlap = retrieved.Intersect(relevant).Count();

            retrievalPrecision.Add(retrieved.Count == 0 ? 1 : (double)overlap / retrieved.Count);
            retrievalRecall.Add(relevant.Count == 0 ? 1 : (double)overlap / relevant.Count);
        }

        if (evalPrompt.ExpectedTools is not null)
        {
            HashSet<string> executed =
                [.. tools.Values
                    .Where(tool => tool.ReceivedInputs.Count > 0)
                    .Select(tool => tool.Name)];

            toolSelection.Add(executed.SetEquals(evalPrompt.ExpectedTools) ? 1 : 0);
        }

        if (evalPrompt.ShouldRefuse is bool shouldRefuse)
        {
            bool refused = outcome.Status is AgentStatus.Refused;

            refusalCorrectness.Add(refused == shouldRefuse ? 1 : 0);
        }
    }

    Dictionary<string, double> scores = [];

    AddAverage(scores, "taskCompletion", taskCompletion);
    AddAverage(scores, "groundedness", groundedness);
    AddAverage(scores, "retrievalPrecision", retrievalPrecision);
    AddAverage(scores, "retrievalRecall", retrievalRecall);
    AddAverage(scores, "toolSelection", toolSelection);
    AddAverage(scores, "refusalCorrectness", refusalCorrectness);

    // A revision was effective when the Judge rejected at least once and the run still ended
    // answered, carrying the facts the task needed — the retry existed AND succeeded.
    bool scriptedARejection = (evalCase.JudgeScores ?? [])
        .Any(score => double.TryParse(score, out double parsed) && parsed < 0.7);

    if (scriptedARejection)
    {
        bool effective = judgeAsked >= 2
            && finalStatus is AgentStatus.Responded
            && finalCarriedItsFacts;

        scores["revisionEffectiveness"] = effective ? 1 : 0;
    }

    return scores;
}

static void AddAverage(Dictionary<string, double> scores, string metric, List<double> samples)
{
    if (samples.Count > 0)
    {
        scores[metric] = samples.Average();
    }
}

static string HashGoldenSet(string goldenPath)
{
    IEnumerable<string> files = Directory
        .EnumerateFiles(goldenPath, "*.json")
        .OrderBy(path => path, StringComparer.Ordinal);

    using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

    foreach (string file in files)
    {
        hash.AppendData(File.ReadAllBytes(file));
    }

    return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant()[..12];
}

static string FindRepositoryRoot()
{
    DirectoryInfo? directory = new(AppContext.BaseDirectory);

    while (directory is not null
        && Directory.Exists(Path.Combine(directory.FullName, "evals")) is false)
    {
        directory = directory.Parent;
    }

    return directory?.FullName
        ?? throw new InvalidOperationException(
            "could not find the repository root (a folder containing 'evals')");
}
