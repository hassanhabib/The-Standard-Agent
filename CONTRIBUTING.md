# Contributing

This repository follows [The Standard](https://github.com/hassanhabib/The-Standard) — the
architecture, the testing discipline, and the contribution practices. The machine-readable form
of those practices lives in
[the-standard-skills](https://github.com/hassanhabib/the-standard-skills); what follows is the
short version a contributor needs on day one.

## The workflow

Fork → branch → FAIL/PASS commits → pull request. Contributors never push to this repository
directly; only the maintainer merges.

## Branches

```
users/[username]/[CATEGORY]-[entity]-[action]
```

`[CATEGORY]` is UPPER-CASE from the Standard category list (FOUNDATIONS, BROKERS,
ORCHESTRATIONS, COORDINATIONS, ACCEPTANCE, DOCUMENTATION, CONFIG, INFRA, RELEASES, …), sized
MAJOR / MEDIUM / MINOR when modifying existing work (5+ / 3–4 / 1–2 tests). One operation per
branch.

```
users/hassanhabib/FOUNDATIONS-approval-add
users/hassanhabib/MINOR-DATA-agent-effect-add-scope
```

## Commits

Test-driven work produces exactly two commits per behavior:

```
[FAIL]: ShouldRefuseAnActNothingNamedAsync
[PASS]: The Perimeter Refuses An Act Nothing Named
```

A `[FAIL]` commit contains one test, **run and observed failing** — a test that never showed
red proves nothing. A `[PASS]` commit contains the implementation, with the **full suite
observed green** before committing. Non-TDD work commits as
`CATEGORY: Description In Pascal Case`.

Two house rules on top: a fix sweeps its class — grep for the same shape elsewhere and say so
in the commit — and a test written after its implementation is sabotage-verified (break the
code, watch the test fail, restore) with the commit saying so.

## The bar a change must clear

The build enforces most of this; the rest is reviewed:

- **Zero warnings.** `TreatWarningsAsErrors` is on and stays on.
- **All tests green, all four readiness profiles certified:**

  ```bash
  dotnet test
  dotnet run --project Standard.Agents.Conformance -- --profile Critical
  ```

- **The tier rules hold** — `TierDisciplineTests` enforces the 2–3 rule, tier adjacency, and
  that no broker sits above the foundation tier. Brokers carry no logic and no unit tests.
- **The capability triad is complete** — Local (`.X`), External (`.UseX`), Custom (`.OnX`) —
  or waived with a stated reason; the matrix test fails the build otherwise.
- **A conformance vector, proven able to fail,** for any behavior another implementation of
  the [spec](https://github.com/hassanhabib/The-Standard-Agent-Specs) must reproduce.
- **Versioning per Standard Versioning** — `model . service/routine . fix/config . build` —
  applied in the release commit, not per PR.

## Pull requests

Titled `CATEGORY: Entity - Description`, one category of work per PR, CI green before review.
The description says what the change does and how you proved it; screenshots or pasted runner
output are welcome. Link issues with `Closes #N` where one applies.

## Questions

Open a discussion or an issue — a well-described problem is halfway solved. For anything
security-shaped, use [SECURITY.md](SECURITY.md) instead of a public issue.
