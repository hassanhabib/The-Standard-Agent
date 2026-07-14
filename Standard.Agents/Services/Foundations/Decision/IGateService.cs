namespace Standard.Agents.Services.Foundations.Decision;

public interface IGateService
{
    ValueTask<string> ScreenAsync(string gatePrompt, string input);
}
