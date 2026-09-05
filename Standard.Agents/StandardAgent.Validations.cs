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
    // An endpoint is the base the route is appended to, so its shape is load-bearing. Without
    // the trailing '/' .NET resolves the route against the parent ('https://host/v1' reaches
    // 'https://host/chat/completions'), and a base that already names chat/completions reaches
    // 'v1/chat/chat/completions' — the URL hosting.md taught (principal review 2026-09-04,
    // F-12). Both fail at the first prompt with a 404 that blames the provider.
    private static void ValidateApiUrl(string apiUrl)
    {
        bool isAbsoluteHttp =
            Uri.TryCreate(apiUrl, UriKind.Absolute, out Uri? endpoint)
                && endpoint.Scheme is "http" or "https";

        bool namesTheRoute =
            apiUrl.TrimEnd('/').EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase);

        if (isAbsoluteHttp is false || apiUrl.EndsWith('/') is false || namesTheRoute)
        {
            throw new InvalidAgentApiUrlException(
                message:
                    "Invalid agent API URL. An endpoint is the base the route is appended to: an "
                        + "absolute http(s) URL ending with '/', such as "
                        + "https://api.peerllm.com/v1/, that does not name chat/completions "
                        + "itself.");
        }
    }

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
