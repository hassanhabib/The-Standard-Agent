// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Services.Foundations.Knowledges;

// Retrieval answers "what is relevant to this task", not "what exists" (SPEC.md §4.2).
//
// What was here before matched the WHOLE prompt literally against WHOLE documents. A natural
// question never appears verbatim inside its own answer, so it retrieved nothing for exactly the
// queries a user asks — the feature looked configured and returned empty every time.
//
// This is lexical scoring: terms in common, rarer terms worth more, shorter passages preferred
// so a long document cannot win by being long. It is deliberately dependency-free, because it is
// the Local mode; a broker backed by full-text search or embeddings is the External one, and the
// contract above it does not change either way (§9).
public partial class KnowledgeService
{
    private const int PassageWordCount = 120;
    private const int PassageStrideWords = 60;

    private static readonly char[] TermSeparators =
        [' ', '\t', '\n', '\r', '.', ',', ';', ':', '!', '?', '"', '\'', '(', ')', '[', ']', '-', '/'];

    // Words too common to say anything about relevance. Kept short on purpose: a longer list is
    // a language model of its own, and this is meant to stay readable.
    private static readonly HashSet<string> NoiseTerms =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "a", "an", "and", "are", "as", "at", "be", "but", "by", "can", "did", "do", "does",
            "for", "from", "had", "has", "have", "how", "i", "in", "is", "it", "its", "me", "my",
            "of", "on", "or", "our", "so", "that", "the", "their", "them", "then", "there",
            "these", "they", "this", "to", "was", "we", "what", "when", "where", "which", "who",
            "why", "will", "with", "you", "your"
        };

    private static string[] Terms(string text) =>
        [.. text
            .Split(TermSeparators, StringSplitOptions.RemoveEmptyEntries)
            .Select(term => term.ToLowerInvariant())
            .Where(term => term.Length > 1 && NoiseTerms.Contains(term) is false)];

    // Overlapping windows, so an answer that straddles a boundary is still found whole by one of
    // them. The cost is that two neighbouring passages may both match; the ranking then picks
    // whichever carries more of the query.
    private static IEnumerable<string> Passages(string document)
    {
        string[] words = document.Split(
            (char[]?)null,
            StringSplitOptions.RemoveEmptyEntries);

        if (words.Length <= PassageWordCount)
        {
            yield return document.Trim();

            yield break;
        }

        for (int start = 0; start < words.Length; start += PassageStrideWords)
        {
            yield return string.Join(' ', words.Skip(start).Take(PassageWordCount));

            if (start + PassageWordCount >= words.Length)
            {
                break;
            }
        }
    }

    // Score = how many query terms appear, each weighted by how rare it is across the corpus,
    // divided by the square root of the passage length. Rarity is what makes "refund" count for
    // more than "policy" when every document mentions policy; the length penalty is what stops a
    // long passage winning simply by containing more words.
    private static double Score(
        string[] queryTerms,
        string passage,
        IReadOnlyDictionary<string, int> documentFrequency,
        int documentCount)
    {
        string[] passageTerms = Terms(passage);

        if (passageTerms.Length == 0)
        {
            return 0;
        }

        var passageTermSet = new HashSet<string>(passageTerms, StringComparer.OrdinalIgnoreCase);
        double score = 0;

        foreach (string queryTerm in queryTerms.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (passageTermSet.Contains(queryTerm) is false)
            {
                continue;
            }

            int seenIn = documentFrequency.TryGetValue(queryTerm, out int frequency)
                ? frequency
                : 1;

            score += Math.Log(1 + ((double)documentCount / seenIn));
        }

        return score / Math.Sqrt(passageTerms.Length);
    }
}
