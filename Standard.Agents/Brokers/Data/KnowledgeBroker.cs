// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Brokers.Data;

// Knowledge is what the agent can look up now — Theory Ch.5. The resource here is
// a documents directory, and the index is the filesystem.
//
// This is the default, not the design. A real deployment points IKnowledgeBroker at
// a vector store or a search API. The point of shipping a working one is that
// "retrieved content is Data" stays a real path rather than a claim: an agent with
// documents on disk genuinely retrieves, and swapping in embeddings changes the
// broker and nothing else.
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

        // The broker owns its resource, so it brings it into existence — the same
        // reason StorageBroker calls Database.Migrate() in its constructor. It also
        // keeps the read path free of an existence check, which would be flow control.
        Directory.CreateDirectory(this.knowledgePath);
    }

    public async ValueTask<IReadOnlyList<string>> SelectKnowledgeAsync(string query)
    {
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

            // Substring matching is the honest limit of a filesystem index. It is
            // not semantic and does not pretend to be — that is what swapping the
            // broker for a vector store buys.
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
