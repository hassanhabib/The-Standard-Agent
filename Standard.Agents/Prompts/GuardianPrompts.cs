// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Reflection;

namespace Standard.Agents.Prompts;

internal static class GuardianPrompts
{
    private const string GateResourceName = "Standard.Agents.Prompts.Guardians.gate.md";
    private const string JudgeResourceName = "Standard.Agents.Prompts.Guardians.judge.md";

    internal static string Gate => Read(GateResourceName);

    internal static string Judge => Read(JudgeResourceName);

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
