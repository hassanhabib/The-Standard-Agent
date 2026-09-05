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

    // Spend is the token count times the rate, so a cost bound with no positive rate computes
    // zero forever and never trips. The framework cannot know what a model costs and so cannot
    // fill the rate in; it can refuse the contradiction of a dollar bound on a model declared
    // free. Token and wall-clock bounds need no price and are not checked here.
    private static void ValidateBudget(decimal? maxCostUsd, decimal costPerThousandTokens)
    {
        if (maxCostUsd is not null && costPerThousandTokens <= 0m)
        {
            throw new InvalidAgentBudgetException(
                message:
                    "Invalid agent budget: a cost bound (maxCostUsd) needs a positive "
                        + "costPerThousandTokens. Cost is the token count times that rate, so at "
                        + "zero the bound can never trip. Pass your model's rate, or bound by "
                        + "maxTokens instead.");
        }
    }
}
