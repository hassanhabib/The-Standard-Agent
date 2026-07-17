// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Clients.Agents.Exceptions;

namespace Standard.Agents;

public sealed partial class StandardAgent
{
                            private void ValidateComposition()
    {
                if (this.generatorBroker is null && this.brainSettings is null)
        {
            throw new InvalidAgentCompositionException(
                message:
                    "Agent has no brain. Call Brain(apiUrl, apiKey, model) "
                        + "or UseGenerator(broker) before processing a prompt.");
        }

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
