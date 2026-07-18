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
        string[] documents = Directory.Exists(this.knowledgePath)
            ? await Task.WhenAll(
                Directory.EnumerateFiles(
                    this.knowledgePath, this.searchPattern, SearchOption.AllDirectories)
                        .OrderBy(documentPath => documentPath, StringComparer.Ordinal)
                        .Select(path => File.ReadAllTextAsync(path)))
            : [];

        return documents
            .Where(document => document.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(this.maxResults)
            .ToList();
    }
}
