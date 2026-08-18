// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Brokers.Usages;
using Standard.Agents.Models.Foundations.Usages;

namespace Standard.Agents.Services.Foundations.Usages;

// What a model call consumed — the question Decision could not answer.
//
// Inference could say what the model SAID and not what it COST. On the native protocol the
// provider reports usage in its response and the numbers were lifted out as they went past; on
// the text protocol there was nobody to ask, so PromptTokens and CompletionTokens stayed zero and
// .Budget(maxTokens:) / .Budget(maxCostUsd:) never tripped at all. A budget that silently does
// not apply is worse than no budget, because it is claimed in the profile.
//
// Measuring belongs to Decision because a token is the model's unit. Deciding whether to CONTINUE
// belongs to the loop, which is where the budget already lives.
public partial class UsageService : IUsageService
{
    private readonly IUsageBroker usageBroker;
    private readonly ILoggingBroker loggingBroker;

    public UsageService(
        IUsageBroker usageBroker,
        ILoggingBroker loggingBroker)
    {
        this.usageBroker = usageBroker;
        this.loggingBroker = loggingBroker;
    }

    public ValueTask<AgentUsage> MeasureAsync(string prompt, string completion) =>
    TryCatch(async () =>
    {
        ValidateText(prompt, nameof(prompt));
        ValidateText(completion, nameof(completion));

        int promptTokens = await this.usageBroker.CountTokensAsync(prompt);
        int completionTokens = await this.usageBroker.CountTokensAsync(completion);

        ValidateCount(promptTokens);
        ValidateCount(completionTokens);

        return new AgentUsage(
            PromptTokens: promptTokens,
            CompletionTokens: completionTokens,
            IsEstimated: true);
    });
}
