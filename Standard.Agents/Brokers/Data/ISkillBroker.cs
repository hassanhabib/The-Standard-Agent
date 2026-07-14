namespace Standard.Agents.Brokers.Data;

public interface ISkillBroker
{
    ValueTask<string> SelectSkillsAsync();
}
