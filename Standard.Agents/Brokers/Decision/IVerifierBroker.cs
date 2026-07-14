namespace Standard.Agents.Brokers.Decision;

public interface IVerifierBroker
{
    ValueTask<double> VerifyAsync(string systemPrompt, string candidate);
}
