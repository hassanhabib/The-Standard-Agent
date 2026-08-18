// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Brokers.Usages;

// The liaison to whatever knows how to count tokens: a tokenizer in the box, a provider's own
// counting endpoint, or a vocabulary shipped with a model.
public interface IUsageBroker
{
    /// <summary>
    /// How many tokens the given text occupies. Returns 0 for empty text.
    /// </summary>
    ValueTask<int> CountTokensAsync(string text);
}
