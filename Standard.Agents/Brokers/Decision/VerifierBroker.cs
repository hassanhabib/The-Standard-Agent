namespace Standard.Agents.Brokers.Decision;

// STUB — the Judge's verifier/scoring model. Today approves everything (1.0); swap
// the body for a real verifier (reward model / LLM-as-judge). The guardian for
// safety-critical actions must be a distinct, trusted mind — never the Brain.
public sealed class VerifierBroker : IVerifierBroker
{
    public ValueTask<double> VerifyAsync(string systemPrompt, string candidate) =>
        ValueTask.FromResult(1.0);
}
