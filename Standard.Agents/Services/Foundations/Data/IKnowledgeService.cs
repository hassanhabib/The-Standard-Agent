namespace Standard.Agents.Services.Foundations.Data;

public interface IKnowledgeService
{
    ValueTask<IReadOnlyList<string>> RetrieveKnowledgeAsync(string query);
}
