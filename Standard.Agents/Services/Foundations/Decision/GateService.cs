using Standard.Agents.Brokers.Decision;

namespace Standard.Agents.Services.Foundations.Decision;

// Foundation over ONE broker (the classifier). The agent's conscience at the front
// door — screens the input to accept, refuse, or route it. Not the Brain.
public sealed class GateService : IGateService
{
    private readonly IClassifierBroker classifierBroker;

    public GateService(IClassifierBroker classifierBroker) =>
        this.classifierBroker = classifierBroker;

    public async ValueTask<string> ScreenAsync(string gatePrompt, string input) =>
        await this.classifierBroker.ClassifyAsync(gatePrompt, input);
}
