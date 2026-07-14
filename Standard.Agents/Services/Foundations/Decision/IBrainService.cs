namespace Standard.Agents.Services.Foundations.Decision;

public interface IBrainService
{
    ValueTask<string> GenerateAsync(string systemPrompt, string userPrompt);
}
