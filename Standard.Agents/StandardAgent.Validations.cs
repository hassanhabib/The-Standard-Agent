// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Clients.Agents.Exceptions;

namespace Standard.Agents;

public sealed partial class StandardAgent
{
    // Validates what each DEFAULT broker needs, not what the agent "should" have. A
    // swapped-in broker needs no settings at all — that is the whole point of Use*,
    // and it is how the conformance harness composes an agent with no endpoint.
    //
    // Each check names the missing piece, so a composition mistake reads as itself
    // rather than as a null-reference from inside a broker constructor.
    private void ValidateComposition()
    {
        // Theory Ch.5: "An agent has one brain." Zero is not a valid count.
        if (this.generatorBroker is null && this.brainSettings is null)
        {
            throw new InvalidAgentCompositionException(
                message:
                    "Agent has no brain. Call Brain(apiUrl, apiKey, model) "
                        + "or UseGenerator(broker) before processing a prompt.");
        }

        // Gate and Judge fall back to the Brain's settings, so they are only
        // unsatisfiable when the Brain was swapped in rather than configured.
        if (this.classifierBroker is null && this.gateSettings is null && this.brainSettings is null)
        {
            throw new InvalidAgentCompositionException(
                message:
                    "Agent has no gate. Call Gate(apiUrl, apiKey, model) or "
                        + "UseGate(broker) — a swapped-in brain leaves the gate "
                        + "nothing to fall back to.");
        }

        if (this.verifierBroker is null && this.judgeSettings is null && this.brainSettings is null)
        {
            throw new InvalidAgentCompositionException(
                message:
                    "Agent has no judge. Call Judge(apiUrl, apiKey, model) or "
                        + "UseJudge(broker) — a swapped-in brain leaves the judge "
                        + "nothing to fall back to.");
        }
    }
}
