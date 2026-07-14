namespace Standard.Agents.Services.Foundations.Direction;

public interface IExternalToolService
{
    ValueTask<string> CallAsync(string toolName, string input);
}
