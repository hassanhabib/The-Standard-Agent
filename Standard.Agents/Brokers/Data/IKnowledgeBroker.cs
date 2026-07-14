namespace Standard.Agents.Brokers.Data;

public interface IKnowledgeBroker
{
    ValueTask<IReadOnlyList<string>> SelectKnowledgeAsync(string query);
}
