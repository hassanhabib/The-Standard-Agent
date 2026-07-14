namespace Standard.Agents.Services.Foundations.Direction;

public interface IInternalToolService
{
    bool Handles(string toolName);

    ValueTask<string> RunAsync(string toolName, string input);
}
