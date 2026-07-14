using Standard.Agents.Brokers.Decision;

namespace Standard.Agents.Services.Foundations.Decision;

// Foundation over ONE broker (the verifier). The agent's conscience at the back door
// — scores a candidate before it leaves. For safety-critical output the guardian
// must be a distinct, trusted mind, never the Brain.
public sealed class JudgeService : IJudgeService
{
    private readonly IVerifierBroker verifierBroker;

    public JudgeService(IVerifierBroker verifierBroker) =>
        this.verifierBroker = verifierBroker;

    public async ValueTask<double> EvaluateAsync(string judgePrompt, string candidate) =>
        await this.verifierBroker.VerifyAsync(judgePrompt, candidate);
}
