using Standard.Agents.Brokers.Data;

namespace Standard.Agents.Services.Foundations.Data;

// Foundation over ONE broker (the knowledge index). On-demand retrieval for the
// current task — what the agent can look up.
public sealed class KnowledgeService : IKnowledgeService
{
    private readonly IKnowledgeBroker knowledgeBroker;

    public KnowledgeService(IKnowledgeBroker knowledgeBroker) =>
        this.knowledgeBroker = knowledgeBroker;

    public async ValueTask<IReadOnlyList<string>> RetrieveKnowledgeAsync(string query) =>
        await this.knowledgeBroker.SelectKnowledgeAsync(query);
}
