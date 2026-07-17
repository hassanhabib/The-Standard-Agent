// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Clients.Agents.Exceptions;

namespace Standard.Agents;

public sealed partial class StandardAgent
{
    // An agent with no Brain is not an agent. Theory Ch.5: "An agent has one brain."
    // Zero is not a valid count, and failing at composition says so at the moment the
    // mistake was made rather than as a null-reference on the first prompt.
    //
    // A swapped-in generator counts — UseGenerator(broker) is a Brain.
    private void ValidateComposition()
    {
        if (this.generatorBroker is null && this.brainSettings is null)
        {
            throw new InvalidAgentCompositionException(
                message:
                    "Agent has no brain. Call Brain(apiUrl, apiKey, model) "
                        + "or UseGenerator(broker) before processing a prompt.");
        }
    }
}
