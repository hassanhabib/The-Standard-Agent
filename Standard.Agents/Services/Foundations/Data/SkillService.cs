using Standard.Agents.Brokers.Data;

namespace Standard.Agents.Services.Foundations.Data;

// Foundation over ONE broker (the skill broker). Business language: Select → Retrieve.
public sealed class SkillService : ISkillService
{
    private readonly ISkillBroker skillBroker;

    public SkillService(ISkillBroker skillBroker) =>
        this.skillBroker = skillBroker;

    public async ValueTask<string> RetrieveSkillsAsync() =>
        await this.skillBroker.SelectSkillsAsync();
}
