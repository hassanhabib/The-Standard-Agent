// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Reflection;

namespace Standard.Agents.Prompts;

internal static class GuardianPrompts
{
    private const string GatePolicyResourceName = "Standard.Agents.Prompts.Guardians.gate.policy.md";
    private const string GateContractResourceName = "Standard.Agents.Prompts.Guardians.gate.contract.md";
    private const string JudgePolicyResourceName = "Standard.Agents.Prompts.Guardians.judge.policy.md";
    private const string JudgeContractResourceName = "Standard.Agents.Prompts.Guardians.judge.contract.md";

    // The policy is the replaceable part (what to screen or score); a consumption skill
    // can supply its own. The contract is the output protocol (allow/refuse, the score) the
    // broker parses, so it is framework-owned and always appended, never replaced.
    internal static string GatePolicy => Read(GatePolicyResourceName);

    internal static string GateContract => Read(GateContractResourceName);

    internal static string JudgePolicy => Read(JudgePolicyResourceName);

    internal static string JudgeContract => Read(JudgeContractResourceName);

    private static string Read(string resourceName)
    {
        Assembly assembly = typeof(GuardianPrompts).Assembly;

        using Stream stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded guardian prompt '{resourceName}' was not found.");

        using StreamReader reader = new(stream);

        return reader.ReadToEnd();
    }
}
