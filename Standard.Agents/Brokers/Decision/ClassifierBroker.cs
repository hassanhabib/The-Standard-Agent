namespace Standard.Agents.Brokers.Decision;

// STUB — the Gate's classifier/guardrail model. Today allows everything; swap the
// body for a real classifier (or point it at the same LLM as the Generator).
public sealed class ClassifierBroker : IClassifierBroker
{
    public ValueTask<string> ClassifyAsync(string systemPrompt, string input) =>
        ValueTask.FromResult("allow");
}
