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
    }
        }
