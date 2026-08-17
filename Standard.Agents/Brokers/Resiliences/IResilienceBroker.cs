// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Brokers.Resiliences;

public interface IResilienceBroker
{
    ValueTask<T> ExecuteAsync<T>(Func<ValueTask<T>> operation);
}
