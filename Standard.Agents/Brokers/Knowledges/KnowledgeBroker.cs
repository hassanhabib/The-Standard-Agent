// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Brokers.Knowledges;

public sealed class KnowledgeBroker : IKnowledgeBroker
{
    private readonly string knowledgePath;
    private readonly string searchPattern;
    private readonly int maxResults;

    public KnowledgeBroker(
        string knowledgePath,
        string searchPattern,
        int maxResults)
    {
        this.knowledgePath = Path.GetFullPath(knowledgePath);
        this.searchPattern = searchPattern;
        this.maxResults = maxResults;
    }

    public async ValueTask<IReadOnlyList<string>> SelectKnowledgeAsync(string query)
    {
        if (Directory.Exists(this.knowledgePath) is false)
        {
            return [];
        }

        List<string> matches = [];

        IOrderedEnumerable<string> documentPaths =
            Directory.EnumerateFiles(
                this.knowledgePath,
                this.searchPattern,
                SearchOption.AllDirectories)
                    .OrderBy(documentPath => documentPath, StringComparer.Ordinal);

        foreach (string documentPath in documentPaths)
        {
            string document = await File.ReadAllTextAsync(documentPath);

            if (document.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                matches.Add(document);
            }

            if (matches.Count == this.maxResults)
            {
                break;
            }
        }

        return matches;
    }
}
