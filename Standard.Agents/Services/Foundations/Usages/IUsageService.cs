// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Foundations.Usages;

namespace Standard.Agents.Services.Foundations.Usages;

public interface IUsageService
{
    /// <summary>
    /// What one model call consumed, counted locally. Returned with
    /// <see cref="AgentUsage.IsEstimated"/> set, because a counted number is an estimate of what
    /// the provider will bill.
    /// </summary>
    ValueTask<AgentUsage> MeasureAsync(string prompt, string completion);
}
