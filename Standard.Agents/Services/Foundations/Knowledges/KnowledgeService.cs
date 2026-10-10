// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Files;
using Standard.Agents.Brokers.Knowledges;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Models.Foundations.Knowledges;

namespace Standard.Agents.Services.Foundations.Knowledges;

public partial class KnowledgeService : IKnowledgeService
{
    private readonly IKnowledgeBroker? knowledgeBroker;
    private readonly ISourcedKnowledgeBroker? sourcedKnowledgeBroker;
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
        ISourcedKnowledgeBroker sourcedKnowledgeBroker,
        ILoggingBroker loggingBroker)
    {
        this.sourcedKnowledgeBroker = sourcedKnowledgeBroker;
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

        IReadOnlyList<KnowledgeResult> knowledgeResults = await SelectSourcedKnowledgeAsync(query);

        IReadOnlyList<string> documents =
            [.. knowledgeResults.Select(knowledgeResult => knowledgeResult.Text)];

        return documents;
    });

    public ValueTask<IReadOnlyList<KnowledgeResult>> RetrieveSourcedKnowledgeAsync(string query) =>
    TryCatch(async () =>
    {
        ValidateQuery(query);

        return await SelectSourcedKnowledgeAsync(query);
    });

    // One retrieval behind both doors, so the plain one can never rank or filter differently from
    // the sourced one: it is the same passages with what travels beside them left off.
    private async ValueTask<IReadOnlyList<KnowledgeResult>> SelectSourcedKnowledgeAsync(string query)
    {
        if (this.fileBroker is not null)
        {
            return await SelectKnowledgeFromFilesAsync(this.fileBroker, query);
        }

        if (this.sourcedKnowledgeBroker is not null)
        {
            return await this.sourcedKnowledgeBroker.SelectSourcedKnowledgeAsync(query);
        }

        IReadOnlyList<string> documents = await this.knowledgeBroker!.SelectKnowledgeAsync(query);

        return [.. documents.Select(LiftUnsourced)];
    }

    // A plain broker never said where a passage came from (SPEC.md §4.1), so the lifted passage
    // claims nothing: no score, no source, and therefore never cited.
    private static KnowledgeResult LiftUnsourced(string document)
    {
        return new KnowledgeResult
        {
            Text = document,
            Score = null,
            Source = string.Empty
        };
    }

    private async ValueTask<IReadOnlyList<KnowledgeResult>> SelectKnowledgeFromFilesAsync(
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
        List<string> documentSources = [];

        foreach (string documentPath in documentPaths)
        {
            documents.Add(await fileBroker.ReadFileAsync(documentPath));
            documentSources.Add(SourceOf(documentPath));
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
            .. documents.SelectMany((document, index) => Passages(document)
                .Select(passage => new
                {
                    Passage = passage,
                    Source = documentSources[index],
                    Score = Score(queryTerms, passage, documentFrequency, documents.Count)
                }))
                // A zero score means the passage carries no query term at all, so it is never a
                // match however low the floor is set. Silence is the right answer when nothing
                // is relevant — returning the corpus instead is how a retriever becomes noise.
                .Where(scored => scored.Score > 0 && scored.Score >= this.minimumScore)
                .OrderByDescending(scored => scored.Score)
                .Take(this.maxResults)
                .Select(scored => new KnowledgeResult
                {
                    Text = scored.Passage,
                    Score = scored.Score,
                    Source = scored.Source
                })
        ];
    }

    // Relative to the knowledge folder and with forward slashes whatever the platform, so the
    // same document is cited the same way on every machine that serves it.
    private string SourceOf(string documentPath) =>
        Path.GetRelativePath(this.knowledgePath, documentPath).Replace('\\', '/');
}
