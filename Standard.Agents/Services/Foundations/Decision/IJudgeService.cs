namespace Standard.Agents.Services.Foundations.Decision;

public interface IJudgeService
{
    ValueTask<double> EvaluateAsync(string judgePrompt, string candidate);
}
