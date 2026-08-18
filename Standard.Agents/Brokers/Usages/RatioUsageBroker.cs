// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Brokers.Usages;

// The Local mode: a tokenizer in the box, so a budget works against any endpoint without asking
// the host to install anything.
//
// It is an ESTIMATE and says so — every number it produces is marked IsEstimated, because a
// budget you can enforce and a number you can bill against are not the same claim. Word-aware
// rather than a flat characters/4: whitespace is not billed, and a long word costs more than a
// short one, which is where a flat ratio drifts worst on code and identifiers.
//
// A host that needs exactness supplies the provider's own tokenizer through .UseUsage.
public sealed class RatioUsageBroker : IUsageBroker
{
    // Roughly the average characters-per-token for English BPE vocabularies. It is a ratio, not
    // a rule, which is the whole reason the result is flagged as estimated.
    private const double CharactersPerToken = 4.0;

    public ValueTask<int> CountTokensAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return ValueTask.FromResult(0);
        }

        string[] words = text.Split(
            (char[]?)null,
            StringSplitOptions.RemoveEmptyEntries);

        int tokens = 0;

        foreach (string word in words)
        {
            // Every word costs at least one token however short it is, and a longer one splits
            // into subwords rather than scaling smoothly with its length.
            tokens += Math.Max(1, (int)Math.Ceiling(word.Length / CharactersPerToken));
        }

        return ValueTask.FromResult(tokens);
    }
}
