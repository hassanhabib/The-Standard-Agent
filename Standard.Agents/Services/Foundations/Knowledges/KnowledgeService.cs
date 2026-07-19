// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Files;
using Standard.Agents.Brokers.Knowledges;
using Standard.Agents.Brokers.Loggings;

namespace Standard.Agents.Services.Foundations.Knowledges;

public partial class KnowledgeService : IKnowledgeService
{
    private readonly IKnowledgeBroker? knowledgeBroker;
    private readonly IFileBroker? fileBroker;
    private readonly string knowledgePath;
    private readonly string searchPattern;
    private readonly int maxResults;
    private readonly ILoggingBroker loggingBroker;

    public KnowledgeService(
        IKnowledgeBroker knowledgeBroker,
        ILoggingBroker loggingBroker)
    {
        this.knowledgeBroker = knowledgeBroker;
        this.knowledgePath = string.Empty;
        this.searchPattern = string.Empty;
        this.loggingBroker = loggingBroker;
    }

    public KnowledgeService(
        IFileBroker fileBroker,
        string knowledgePath,
        string searchPattern,
        int maxResults,
        ILoggingBroker loggingBroker)
    {
        this.fileBroker = fileBroker;
        this.knowledgePath = knowledgePath;
        this.searchPattern = searchPattern;
        this.maxResults = maxResults;
        this.loggingBroker = loggingBroker;
    }

    public ValueTask<IReadOnlyList<string>> RetrieveKnowledgeAsync(string query) =>
    TryCatch(async () =>
    {
        ValidateQuery(query);

        return this.fileBroker is not null
            ? await SelectKnowledgeFromFilesAsync(this.fileBroker, query)
            : await this.knowledgeBroker!.SelectKnowledgeAsync(query);
    });

    private async ValueTask<IReadOnlyList<string>> SelectKnowledgeFromFilesAsync(IFileBroker fileBroker, string query)
    {
        if (fileBroker.DirectoryExists(this.knowledgePath) is false)
        {
            return [];
        }

        IOrderedEnumerable<string> documentPaths =
            fileBroker.SelectFiles(this.knowledgePath, this.searchPattern, SearchOption.AllDirectories)
                .OrderBy(documentPath => documentPath, StringComparer.Ordinal);

        List<string> matches = [];

        foreach (string documentPath in documentPaths)
        {
            string document = await fileBroker.ReadFileAsync(documentPath);

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
