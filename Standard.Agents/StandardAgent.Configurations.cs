// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents;

public partial class StandardAgent
{
    /// <summary>
    /// Composes an agent from a JSON document — the whole configurable surface as data, one key
    /// per capability, the same names as the builder verbs. Anything about an agent that is data
    /// can arrive as data: a low-code form, a database row, a request body some platform stored.
    /// Tools stay code because they are code — except MCP, where a tool is a URL, which is data.
    /// An unknown key throws with the key named rather than composing an agent that silently
    /// lacks a control the caller believes is on.
    /// </summary>
    /// <param name="json">The configuration document.</param>
    /// <returns>The composed agent — keep chaining code (tools, delegates) after it.</returns>
    public static StandardAgent FromJson(string json) =>
        throw new NotImplementedException();

    /// <summary>Reads <paramref name="path"/> and composes the agent from its JSON.</summary>
    /// <param name="path">Path to the configuration document (e.g. <c>agent.json</c>).</param>
    /// <returns>The composed agent — keep chaining code (tools, delegates) after it.</returns>
    public static StandardAgent FromJsonFile(string path) =>
        FromJson(File.ReadAllText(path));
}
