using Standard.Agents.Brokers.Decision;

namespace Standard.Agents.Services.Foundations.Decision;

// Foundation over ONE broker (the generator). The one Brain — it produces the
// reasoning. Interpreting that reply into a decision is the Decision orchestration's
// job; this service just generates.
public sealed class BrainService : IBrainService
{
    private readonly IGeneratorBroker generatorBroker;

    public BrainService(IGeneratorBroker generatorBroker) =>
        this.generatorBroker = generatorBroker;

    public async ValueTask<string> GenerateAsync(string systemPrompt, string userPrompt) =>
        await this.generatorBroker.GenerateAsync(systemPrompt, userPrompt);
}
