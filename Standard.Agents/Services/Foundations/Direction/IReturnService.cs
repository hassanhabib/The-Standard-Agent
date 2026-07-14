namespace Standard.Agents.Services.Foundations.Direction;

public interface IReturnService
{
    ValueTask<string> ReturnAsync(string payload);
}
