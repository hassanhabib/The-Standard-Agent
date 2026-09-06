// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

// The live evals: the same agent loop against a real model on PeerLLM and real skills from the
// PeerLLM registry. Where the deterministic evals pin how the loop treats a scripted Brain,
// these measure the Brain and the skills themselves: does LLooMA read a skill and answer from
// it, does it pick the tool the task needs, does it stay off the fabrication the author named.
// Opt-in and outside the PR gate, because a live model is not deterministic and a flake in a
// required gate is a gate nobody trusts (principal review 2026-09-04, F-19). So every prompt is
// sampled several times, a metric is a pass rate over the samples, a threshold is a pass rate
// the case must reach, and every report records which model, which skillset version, which
// framework and when - a score nobody can attribute is a score nobody can investigate.

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Standard.Agents;
using Standard.Agents.Evals.Live;
using Standard.Agents.Models.Clients.Agents;
using Standard.Agents.Models.Orchestrations.Agents;

const string SamplesFlag = "--samples";
const string MaxCasesFlag = "--max-cases";
const string ReportFlag = "--report";
const string KeyFileFlag = "--key-file";

string[] knownMetrics = ["taskCompletion", "groundedness", "toolSelection"];

string apiUrl = Environment.GetEnvironmentVariable("PEERLLM_API_URL") ?? "https://api.peerllm.com/v1/";
string model = Environment.GetEnvironmentVariable("PEERLLM_MODEL") ?? "LLooMA2.0";
string? apiKey = Environment.GetEnvironmentVariable("PEERLLM_API_KEY");

int? samplesOverride = null;
int maxCases = int.MaxValue;
string? reportPath = null;
string? casesPath = null;

for (int index = 0; index < args.Length; index++)
{
    switch (args[index])
    {
        case SamplesFlag:
            samplesOverride = int.Parse(args[++index]);

            break;

        case MaxCasesFlag:
            maxCases = int.Parse(args[++index]);

            break;

        case ReportFlag:
            reportPath = args[++index];

            break;

        case KeyFileFlag:
            apiKey = (await File.ReadAllTextAsync(args[++index])).Trim();

            break;

        default:
            casesPath = args[index];

            break;
    }
}

casesPath ??= Path.Combine(FindRepositoryRoot(), "evals", "live");

// No key, no run - and no green. A live eval that silently skipped is a live eval that never
// measured anything.
if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.WriteLine(
        $"No PeerLLM key. Set PEERLLM_API_KEY, or pass {KeyFileFlag} <path>. Exit 3.");

    return 3;
}

JsonSerializerOptions jsonOptions = new() { PropertyNameCaseInsensitive = true };
string frameworkVersion = typeof(StandardAgent).Assembly.GetName().Version?.ToString() ?? "unknown";
string setHash = HashSet(casesPath);
DateTimeOffset startedOn = DateTimeOffset.UtcNow;
using HttpClient registry = new();

Console.WriteLine($"Live set:   {casesPath}");
Console.WriteLine($"Model:      {model} at {new Uri(apiUrl).Host}");
Console.WriteLine($"Framework:  Standard.Agents {frameworkVersion}");
Console.WriteLine($"Set hash:   {setHash}");
Console.WriteLine();

int passed = 0;
int failed = 0;
List<object> caseReports = [];

foreach (string caseFile in Directory.EnumerateFiles(casesPath, "*.json")
    .OrderBy(path => path, StringComparer.Ordinal)
    .Take(maxCases))
{
    LiveCase liveCase =
        JsonSerializer.Deserialize<LiveCase>(await File.ReadAllTextAsync(caseFile), jsonOptions)!;

    if (liveCase.Skillset.Contains('@') is false)
    {
        failed++;
        Console.WriteLine($"FAIL  {liveCase.Name}");
        Console.WriteLine("        skillset is not pinned to a version (owner/name@N); an unpinned live score cannot be reproduced");

        continue;
    }

    int samples = samplesOverride ?? liveCase.Samples;
    Stopwatch caseClock = Stopwatch.StartNew();

    (Dictionary<string, double> scores, int skillsetVersion, List<string> answers) =
        await ScoreCaseAsync(liveCase, samples);

    List<string> verdicts = [];
    bool caseFailed = false;

    foreach (KeyValuePair<string, double> threshold in liveCase.Thresholds)
    {
        if (knownMetrics.Contains(threshold.Key) is false)
        {
            caseFailed = true;
            verdicts.Add($"unknown metric '{threshold.Key}' — knowable: {string.Join(", ", knownMetrics)}");

            continue;
        }

        if (scores.TryGetValue(threshold.Key, out double score) is false)
        {
            caseFailed = true;
            verdicts.Add($"{threshold.Key}: threshold {threshold.Value:0.00} binds nothing — no prompt carries its golden data");

            continue;
        }

        bool met = score >= threshold.Value;
        caseFailed |= met is false;
        verdicts.Add($"{threshold.Key} = {score:0.00} over {samples} sample(s) (threshold {threshold.Value:0.00}){(met ? "" : "  ← UNMET")}");
    }

    foreach (KeyValuePair<string, double> score in scores
        .Where(score => liveCase.Thresholds.ContainsKey(score.Key) is false))
    {
        verdicts.Add($"{score.Key} = {score.Value:0.00} (unthresholded)");
    }

    if (caseFailed)
    {
        failed++;
        Console.WriteLine($"FAIL  {liveCase.Name}");
    }
    else
    {
        passed++;
        Console.WriteLine($"PASS  {liveCase.Name}");
    }

    Console.WriteLine($"        skillset {liveCase.Skillset} (registry version {skillsetVersion}), {liveCase.Protocol} protocol, {caseClock.Elapsed.TotalSeconds:0}s");

    foreach (string verdict in verdicts)
    {
        Console.WriteLine($"        {verdict}");
    }

    caseReports.Add(new
    {
        liveCase.Name,
        liveCase.Skillset,
        skillsetVersion,
        liveCase.Protocol,
        samples,
        scores,
        liveCase.Thresholds,
        passed = caseFailed is false,
        elapsedSeconds = caseClock.Elapsed.TotalSeconds,
        answers
    });
}

Console.WriteLine();
Console.WriteLine($"{passed} passed, {failed} failed  [{model} at {new Uri(apiUrl).Host}, Standard.Agents {frameworkVersion}, set {setHash}]");

if (reportPath is not null)
{
    var report = new
    {
        startedOn,
        finishedOn = DateTimeOffset.UtcNow,
        model,
        apiHost = new Uri(apiUrl).Host,
        frameworkVersion,
        setHash,
        passed,
        failed,
        cases = caseReports
    };

    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(reportPath))!);

    await File.WriteAllTextAsync(
        reportPath,
        JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));

    Console.WriteLine($"Report:     {Path.GetFullPath(reportPath)}");
}

if (passed + failed == 0)
{
    Console.WriteLine($"No cases discovered in {casesPath}. A run that certifies nothing is not a pass.");

    return 2;
}

return failed == 0 ? 0 : 1;

// Every prompt of the case, sampled the case's number of times through a fresh agent each time,
// so no sample sees another's session or tool history. A metric is the pass rate over samples.
async Task<(Dictionary<string, double> Scores, int SkillsetVersion, List<string> Answers)> ScoreCaseAsync(
    LiveCase liveCase,
    int samples)
{
    List<double> taskCompletion = [];
    List<double> groundedness = [];
    List<double> toolSelection = [];
    List<string> answers = [];
    int skillsetVersion = 0;

    foreach (LivePrompt livePrompt in liveCase.Prompts)
    {
        for (int sample = 0; sample < samples; sample++)
        {
            Dictionary<string, RecordingTool> tools = (liveCase.Tools ?? []).ToDictionary(
                pair => pair.Key,
                pair => new RecordingTool(pair.Key, pair.Value, description: $"The {pair.Key} tool."));

            var skills = new PeerLLMRegistrySkillBroker(registry, liveCase.Skillset, liveCase.Members ?? []);

            StandardAgent agent = new StandardAgent()
                .UseSkills(skills)
                .WithoutMemory()
                .Tools(tools.Values)
                .MaxTurns(liveCase.MaxTurns)
                .Budget(maxTokens: liveCase.MaxTokensPerRun);

            agent = liveCase.Protocol.Equals("native", StringComparison.OrdinalIgnoreCase)
                ? agent.NativeBrain(apiUrl, apiKey!, model, temperature: 0)
                : agent.Brain(apiUrl, apiKey!, model, temperature: 0);

            AgentOutcome outcome = await agent.RunAsync(livePrompt.Prompt);
            skillsetVersion = skills.ResolvedVersion;
            string answer = outcome.Status is AgentStatus.Responded ? outcome.Result : $"[{outcome.Status}] {outcome.Result}";
            answers.Add(answer);

            bool responded = outcome.Status is AgentStatus.Responded;

            if (livePrompt.AnswerMustContain is { Count: > 0 } || livePrompt.AnswerMustContainAny is { Count: > 0 })
            {
                bool carriesAll = (livePrompt.AnswerMustContain ?? [])
                    .All(fact => outcome.Result.Contains(fact, StringComparison.OrdinalIgnoreCase));

                bool carriesAny = livePrompt.AnswerMustContainAny is not { Count: > 0 }
                    || livePrompt.AnswerMustContainAny.Any(fact =>
                        outcome.Result.Contains(fact, StringComparison.OrdinalIgnoreCase));

                taskCompletion.Add(responded && carriesAll && carriesAny ? 1 : 0);
            }

            if (livePrompt.AnswerMustNotContain is { Count: > 0 })
            {
                bool fabricatesNothing = livePrompt.AnswerMustNotContain
                    .All(claim => outcome.Result.Contains(claim, StringComparison.OrdinalIgnoreCase) is false);

                groundedness.Add(fabricatesNothing ? 1 : 0);
            }

            if (livePrompt.ExpectedTools is not null)
            {
                HashSet<string> executed =
                    [.. tools.Values.Where(tool => tool.ReceivedInputs.Count > 0).Select(tool => tool.Name)];

                toolSelection.Add(executed.SetEquals(livePrompt.ExpectedTools) ? 1 : 0);
            }
        }
    }

    Dictionary<string, double> scores = [];
    AddAverage(scores, "taskCompletion", taskCompletion);
    AddAverage(scores, "groundedness", groundedness);
    AddAverage(scores, "toolSelection", toolSelection);

    return (scores, skillsetVersion, answers);
}

static void AddAverage(Dictionary<string, double> scores, string metric, List<double> samples)
{
    if (samples.Count > 0)
    {
        scores[metric] = samples.Average();
    }
}

static string HashSet(string casesPath)
{
    using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

    foreach (string file in Directory.EnumerateFiles(casesPath, "*.json").OrderBy(path => path, StringComparer.Ordinal))
    {
        hash.AppendData(File.ReadAllBytes(file));
    }

    return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant()[..12];
}

static string FindRepositoryRoot()
{
    DirectoryInfo? directory = new(AppContext.BaseDirectory);

    while (directory is not null && Directory.Exists(Path.Combine(directory.FullName, "evals")) is false)
    {
        directory = directory.Parent;
    }

    return directory?.FullName ?? throw new DirectoryNotFoundException("No 'evals' folder above the runner.");
}
