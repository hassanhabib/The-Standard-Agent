#!/usr/bin/env bash
# ---------------------------------------------------------------
# Copyright (c) Hassan Habib All rights reserved.
# Licensed under the The Standard Software License (TSSL)
# ---------------------------------------------------------------
#
# The dependency budget. The core ships three runtime packages, and what THEY bring is what a
# consumer's graph actually carries. This lists every package in the shipped graph, transitively,
# on every target, and compares it to the committed budget in licenses/dependency-budget.txt.
# A package that appears without the budget changing fails the build: growth in the graph is a
# decision someone made on purpose, in a reviewed diff, never a side effect of a version bump
# (principal review 2026-09-04, F-22).
#
#   scripts/dependency-budget.sh            # verify: exit 1 on any difference
#   scripts/dependency-budget.sh --update   # rewrite the budget from the current graph
set -euo pipefail

repository="$(cd "$(dirname "$0")/.." && pwd)"
budget="$repository/licenses/dependency-budget.txt"
project="$repository/Standard.Agents/Standard.Agents.csproj"

# Every "> Package  version" line the SDK prints, top-level and transitive, on every target;
# analyzers are build-time only and never reach a consumer, so they are left out. One line per
# package id and resolved version, sorted, so the file diffs cleanly.
current="$(dotnet list "$project" package --include-transitive \
  | grep -E '^\s*>' \
  | grep -v 'Microsoft.CodeAnalysis.PublicApiAnalyzers' \
  | awk '{print $2 " " $NF}' \
  | sort -u)"

if [ "${1:-}" = "--update" ]; then
  {
    echo "# The shipped dependency graph of Standard.Agents, transitively, across net8.0 and net10.0."
    echo "# Regenerate with scripts/dependency-budget.sh --update; CI fails on any drift from this file."
    echo "$current"
  } > "$budget"

  echo "Budget written: $(echo "$current" | wc -l | tr -d ' ') packages."
  exit 0
fi

expected="$(grep -v '^#' "$budget")"

if [ "$current" != "$expected" ]; then
  echo "::error::The shipped dependency graph differs from licenses/dependency-budget.txt."
  echo "--- budget"
  echo "+++ current"
  diff <(echo "$expected") <(echo "$current") || true
  echo "If the change is intended, run scripts/dependency-budget.sh --update and commit the result."
  exit 1
fi

echo "Dependency budget holds: $(echo "$current" | wc -l | tr -d ' ') packages, as committed."
