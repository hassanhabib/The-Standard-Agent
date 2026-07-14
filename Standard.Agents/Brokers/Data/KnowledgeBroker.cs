namespace Standard.Agents.Brokers.Data;

// STUB — the retrieval resource (vector / search index). Today returns nothing;
// swap the body for a real RAG source.
public sealed class KnowledgeBroker : IKnowledgeBroker
{
    public ValueTask<IReadOnlyList<string>> SelectKnowledgeAsync(string query) =>
        ValueTask.FromResult<IReadOnlyList<string>>([]);
}
