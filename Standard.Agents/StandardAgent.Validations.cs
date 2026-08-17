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
        // A native brain is a brain. Either seam satisfies this — the check exists to catch an
        // agent with no way to think at all, not to prefer one contract over the other.
        bool hasBrain =
            this.generatorBroker is not null
                || this.brainSettings is not null
                || this.generatorBrokerV1 is not null;

        if (hasBrain is false)
        {
            throw new InvalidAgentCompositionException(
                message:
                    "Agent has no brain. Call Brain(apiUrl, apiKey, model), "
                        + "UseGenerator(broker) or NativeBrain(apiUrl, apiKey, model) "
                        + "before processing a prompt.");
        }
    }
}
