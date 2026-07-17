// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Brokers.Direction;

// The External a Core-profile agent has: none.
//
// SPEC.md 8.1 lists Core's Direction as InternalToolService + ToolBroker and
// ReturnService — no External at all. But ActAsync routes an unknown tool to
// External unconditionally, because vector 05 requires the agent to RECOVER from a
// hallucinated tool name rather than die on it. So an agent with no MCP server still
// needs something on the other side of that route.
//
// On #50 ("don't stub brokers"): this is a null object, and the distinction worth
// holding is what it says. #50 killed a ClassifierBroker that returned "allow"
// because it LIED — it claimed screening had happened when none had. This claims
// nothing false. "There is no external tool named x here" is true, and it is the
// answer the Brain needs to try something else.
//
// Configure Mcp(endpointUrl) or UseMcp(broker) to reach a real server.
public sealed class NotConfiguredMcpBroker : IMcpBroker
{
    public ValueTask<string> CallAsync(string name, string input) =>
        ValueTask.FromResult($"[external '{name}' not configured]");
}
