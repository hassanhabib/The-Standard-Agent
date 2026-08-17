// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Brokers.Resiliences;

// The default: run it once, let it fail. Absent configuration the agent behaves exactly as it
// did before retries existed.
public sealed class NotConfiguredResilienceBroker : IResilienceBroker
{
    public ValueTask<T> ExecuteAsync<T>(Func<ValueTask<T>> operation) =>
        operation();
}
