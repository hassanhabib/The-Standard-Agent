#!/usr/bin/env bash
# ---------------------------------------------------------------
# Copyright (c) Hassan Habib All rights reserved.
# Licensed under the The Standard Software License (TSSL)
# ---------------------------------------------------------------
#
# The packed package, consumed. A build that packs is not proof that the package installs: this
# creates a clean console project OUTSIDE the repository tree (so none of the repository's build
# props reach it), installs Standard.Agents.<version>.nupkg from a local feed, compiles the
# README's examples, and runs one deterministic prompt through the real composition - on every
# target the package ships (principal review 2026-09-04, F-27).
#
#   scripts/package-smoke.sh <folder holding Standard.Agents.<version>.nupkg>
set -euo pipefail

# Paths as the .NET SDK reads them: on a Windows shell (Git Bash) that is C:\..., not /c/...,
# and NuGet takes a feed path literally.
native_path() {
  (cd "$1" && (pwd -W 2>/dev/null || pwd))
}

repository="$(cd "$(dirname "$0")/.." && pwd)"
artifacts="$(native_path "${1:-$repository/artifacts}")"
declared="$(sed -n 's/.*<Version>\([^<]*\)<\/Version>.*/\1/p' "$repository/Standard.Agents/Standard.Agents.csproj")"

# NuGet normalizes v1.2.3.0 to 1.2.3 in the package id and file name; a fourth segment survives
# only when it is not zero. The consumer asks for the normalized form, as a consumer would.
version="$(echo "$declared" | sed -E 's/^([0-9]+\.[0-9]+\.[0-9]+)\.0$/\1/')"
package="$artifacts/Standard.Agents.$version.nupkg"

if [ ! -f "$package" ]; then
  echo "::error::No $package to consume. Pack first: dotnet pack Standard.Agents/Standard.Agents.csproj -c Release -o artifacts"
  exit 1
fi

work="${RUNNER_TEMP:-${TMPDIR:-/tmp}}/standard-agents-package-smoke"
rm -rf "$work"
mkdir -p "$work"
work="$(native_path "$work")"

for target in net8.0 net10.0; do
  consumer="$work/consumer-$target"
  mkdir -p "$consumer"

  # The local feed first, so the package under test is the one resolved; nuget.org for its
  # dependencies. A fresh packages folder, so a cached copy of an older build cannot answer.
  cat > "$consumer/nuget.config" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local-artifacts" value="$artifacts" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <config>
    <add key="globalPackagesFolder" value="$consumer/packages" />
  </config>
</configuration>
EOF

  cat > "$consumer/Consumer.csproj" <<EOF
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>$target</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Standard.Agents" Version="$version" />
  </ItemGroup>
</Project>
EOF

  # The README's examples, compiled against the packed assembly exactly as a reader would
  # write them, and one prompt run for real through a deterministic brain.
  cat > "$consumer/Program.cs" <<'EOF'
using Standard.Agents;
using Standard.Agents.Models.Loggings;
using Standard.Agents.Tools;

var agent = new StandardAgent().OnBrain(async (systemPrompt, userPrompt) => "FINAL: 4183");

string answer = await agent.ProcessPromptAsync("What is 47 * 89?");

if (answer != "4183")
{
    Console.Error.WriteLine($"The packed agent answered '{answer}', expected '4183'.");

    return 1;
}

Console.WriteLine($"Standard.Agents answered through the packed package: {answer}");

return 0;

// README: Simple - one line, it is already talking.
static class ReadmeSimple
{
    public static async Task<string> RunAsync(string key)
    {
        var agent = new StandardAgent(apiUrl: "https://api.peerllm.com/v1/", apiKey: key, model: "LLooMA2.0");

        string answer = await agent.ProcessPromptAsync("What is 47 * 89?");

        return answer;
    }
}

// README: Medium - a persona, a tool, a conscience on the way in and out, a memory.
static class ReadmeMedium
{
    public static StandardAgent Compose(string url, string key) =>
        new StandardAgent(url, key, "LLooMA2.0")
            .Skills("Skills")
            .Tool(new CalculatorTool())
            .Gate(apiUrl: url, apiKey: key, model: "LLooMA2.0")
            .Judge(apiUrl: url, apiKey: key, model: "LLooMA2.0")
            .Memory("memory.txt");
}

// README: Enterprise - the same shape, more power.
static class ReadmeEnterprise
{
    public static StandardAgent Compose(string url, string key, Func<string?> currentUserId) =>
        new StandardAgent(url, key, "LLooMA2.0")
            .Skills("Skills")
            .Tool(new CalculatorTool())
            .Gate(apiUrl: url, apiKey: key, model: "LLooMA2.0")
            .Judge(apiUrl: url, apiKey: key, model: "LLooMA2.0")
            .Memory("memory.txt")
            .Redact()
            .LogTo("log.txt", TraceVerbosity.Full)
            .Audit("audit.jsonl")
            .Telemetry("teller-agent")
            .Principal(currentUserId)
            .RequireApproval("wire_transfer");
}

sealed class CalculatorTool : ITool
{
    public string Name => "calculator";

    public string Description => "Evaluates an arithmetic expression.";

    public async ValueTask<string> ExecuteAsync(string input) => input;
}
EOF

  echo "== Consuming Standard.Agents $version on $target =="
  (cd "$consumer" && dotnet run --configuration Release)
done

echo "== Standard.Agents $version installs and runs on net8.0 and net10.0 =="
