# Security Policy

An agent framework's perimeter is only as trustworthy as the process behind it. This is that
process.

## Reporting a vulnerability

**Do not open a public issue for a vulnerability.** Report it privately through
[GitHub Security Advisories](https://github.com/hassanhabib/The-Standard-Agent/security/advisories/new)
so a fix can ship before the details do.

A useful report names:

- the affected surface — a builder routine, a broker, the perimeter, the conformance harness;
- the version (`Standard.Agents X.Y.Z`) it reproduces on;
- what an attacker gains — the framework's own threat categories are a good frame: prompt
  injection, tool authorization bypass, approval-scope escalation, redaction bypass,
  cross-tenant exposure, guardian bypass;
- a reproduction, ideally in the shape of a failing test or conformance vector, which is how
  every defect in this repository is demonstrated.

You will get an acknowledgment within **7 days** and a verdict — confirmed with a fix plan, or
declined with the reasoning — within **30 days**. Credit is yours unless you ask otherwise.

## What is in scope

- The `Standard.Agents` package: the builder surface, the loop, the perimeter
  (authorize → record → approve → run-once → record), guardians, redaction, budgets,
  sessions, and the effect ledger.
- The conformance harness and its vectors, where a hole lets a non-conforming
  implementation certify.
- The release pipeline in `.github/workflows/`.

Out of scope: brokers **you** supply (your storage, your model endpoint, your approval
authority), prompt-level jailbreaks of the model behind the Brain (report those to the model's
provider), and the demo project.

## Supported versions

The latest release line receives security fixes. A fix advances the version per Standard
Versioning (`model.service/routine.fix/config.build`) and ships with the release notes naming
the advisory once it is public.

## What every release already ships

Per [docs/support.md](docs/support.md): a CycloneDX SBOM, signed build provenance, symbols with
embedded sources, a transitive vulnerability audit and secret scan on every change, and a
zero-warning build on the SDK pinned in `global.json`. The package is not Authenticode-signed;
provenance attestation answers "did this come from that source, unmodified."
