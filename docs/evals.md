# Quality Evals

Conformance pins **contracts** — does the framework behave to spec. The evals pin **orchestration
quality** — given what a Brain said, did the loop do its job: carry the facts, cite only what it
was shown, retrieve what was relevant, run the tools the task needed, refuse what should be
refused, and revise when the Judge said so. Both certify on every build; both are deterministic,
so a red run is a finding, never a flake.

**What they do not measure.** The Brain in these runs is scripted: it returns fixed replies and
never reads a prompt or a skill. So the evals cannot detect a worse model, a worse prompt, a
worse skill set, or a drift in a provider's behaviour; they detect the framework treating the
same replies differently than it did yesterday. Model and skill quality is measured against a
live provider, with pinned datasets, repeated samples and statistical thresholds, and that is
opt-in and outside CI by design (principal review 2026-09-04, F-19): it costs money, it is not
deterministic, and a flake in a required gate is a gate nobody trusts. Provider wire contracts
are covered separately and deterministically by the wire-contract tests in the unit suite.

```bash
dotnet run --project Standard.Agents.Evals                     # the framework's golden set
dotnet run --project Standard.Agents.Evals -- path/to/golden   # your own
```

Exit `0` means every threshold in every case is met. Anything else fails the build: `1` when a
case misses a threshold, `2` when the run discovered no cases at all. A run that certifies
nothing is not a pass, so an empty or mistyped golden path fails loudly rather than printing
`0 passed, 0 failed` in green; pass `--allow-empty` to accept an empty set on purpose.

## The metrics

| Metric | Question it answers | Golden data on the prompt |
|---|---|---|
| `taskCompletion` | Did the answer carry the facts the task existed to produce? | `answerMustContain` |
| `groundedness` | Is everything the answer cites among what the Brain was actually shown — and is the known fabrication absent? | `mustCite`, `mustNotClaim` |
| `retrievalPrecision` / `retrievalRecall` | Did retrieval bring the relevant knowledge, and only the relevant knowledge? | `relevantKnowledge` |
| `toolSelection` | Did exactly the tools the task needs run — no more, no fewer? | `expectedTools` |
| `refusalCorrectness` | Was the harmful prompt refused **and** the benign one answered? Both directions, because always-refuse scores perfectly on half of them. | `shouldRefuse` |
| `revisionEffectiveness` | When the Judge rejected a draft, did the loop produce a passing answer that carries the facts? | `judgeScores` with a rejection |

A metric is computed only where the case supplies its golden data. A threshold over a metric no
prompt feeds is an **error**, not a pass — a threshold that binds nothing reads as coverage
while measuring nothing.

## A case

```json
{
  "name": "task-completion-carries-the-facts",
  "skill": "You are a billing assistant. Use the calculator for arithmetic.",
  "tools": { "calculator": "4183" },
  "generatorReplies": [
    "ACTION: calculator: 47*89",
    "FINAL: The total owed on the account is $4,183, from 47 units at $89."
  ],
  "prompts": [
    {
      "prompt": "how much is owed on account 4471",
      "answerMustContain": ["4,183"],
      "expectedTools": ["calculator"]
    }
  ],
  "thresholds": { "taskCompletion": 1.0, "toolSelection": 1.0 }
}
```

The composition under evaluation is the **real** one: knowledge is written to files and served
by the real ranked lexical retrieval, tools run through the real perimeter, guardians run
through the real composition with scripted verdicts, and refusal is judged from the run's
actual `AgentOutcome.Status`. The script makes the Brain deterministic; everything the metrics
measure is the framework's own behavior around it.

## What a score is worth

Every report line carries the framework version and a hash of the golden set —
`6 passed, 0 failed [Standard.Agents 1.6.1.0, set 7b59952e2e0d]` — because a passing score
nobody can attribute is a score nobody can investigate. Change a case and the hash changes;
compare two runs only when both halves match.

Every metric in the shipped set is **proven able to fail**: each was sabotaged (a wrong golden
fact, a citation of something never shown, an always-refusing gate, a judge that never passes)
and observed failing before being trusted to pass. Hold your own golden sets to the same rule.

## Building a golden set for your agent

Point the runner at a folder of cases that use **your** skills, policies and knowledge with a
scripted brain, and wire it into your CI beside your tests. The question it answers is the one
that matters when anything changes: *did v4 of this skillset make the agent worse?* A typo'd
field in a case fails loudly rather than silently deleting the expectation it carried.
