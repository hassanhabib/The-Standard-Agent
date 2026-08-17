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
    private readonly double minimumScore;
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
        ILoggingBroker loggingBroker,
        double minimumScore = 0.0)
    {
        this.fileBroker = fileBroker;
        this.knowledgePath = knowledgePath;
        this.searchPattern = searchPattern;
        this.maxResults = maxResults;
        this.loggingBroker = loggingBroker;
        this.minimumScore = minimumScore;
    }

    public ValueTask<IReadOnlyList<string>> RetrieveKnowledgeAsync(string query) =>
    TryCatch(async () =>
    {
        ValidateQuery(query);

        return this.fileBroker is not null
            ? await SelectKnowledgeFromFilesAsync(this.fileBroker, query)
            : await this.knowledgeBroker!.SelectKnowledgeAsync(query);
    });

    private async ValueTask<IReadOnlyList<string>> SelectKnowledgeFromFilesAsync(
        IFileBroker fileBroker,
        string query)
    {
        if (fileBroker.DirectoryExists(this.knowledgePath) is false)
        {
            return [];
        }

        string[] queryTerms = Terms(query);

        if (queryTerms.Length == 0)
        {
            return [];
        }

        IOrderedEnumerable<string> documentPaths =
            fileBroker.SelectFiles(this.knowledgePath, this.searchPattern, SearchOption.AllDirectories)
                .OrderBy(documentPath => documentPath, StringComparer.Ordinal);

        List<string> documents = [];

        foreach (string documentPath in documentPaths)
        {
            documents.Add(await fileBroker.ReadFileAsync(documentPath));
        }

        // How many documents each term appears in, so a term common to the whole corpus counts
        // for less than one that singles a document out.
        Dictionary<string, int> documentFrequency = new(StringComparer.OrdinalIgnoreCase);

        foreach (string document in documents)
        {
            foreach (string term in Terms(document).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                documentFrequency[term] =
                    documentFrequency.TryGetValue(term, out int count) ? count + 1 : 1;
            }
        }

        return
        [
            .. documents
                .SelectMany(Passages)
                .Select(passage => new
                {
                    Passage = passage,
                    Score = Score(queryTerms, passage, documentFrequency, documents.Count)
                })
                // A zero score means the passage carries no query term at all, so it is never a
                // match however low the floor is set. Silence is the right answer when nothing
                // is relevant — returning the corpus instead is how a retriever becomes noise.
                .Where(scored => scored.Score > 0 && scored.Score >= this.minimumScore)
                .OrderByDescending(scored => scored.Score)
                .Take(this.maxResults)
                .Select(scored => scored.Passage)
        ];
    }
}
