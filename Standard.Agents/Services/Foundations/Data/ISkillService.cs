namespace Standard.Agents.Services.Foundations.Data;

public interface ISkillService
{
    ValueTask<string> RetrieveSkillsAsync();
}
