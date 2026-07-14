namespace Standard.Agents.Brokers.Loggings;

public interface ILogBroker
{
    string LogPath { get; }

    ValueTask ResetAsync();

    ValueTask WriteAsync(string content);
}
