namespace Standard.Agents.Brokers.Loggings;

// Support broker: a thin liaison to the flow-log file. Reset wipes it per prompt;
// Write appends. No flow control, no thinking.
public sealed class LogBroker : ILogBroker
{
    private readonly string logPath;

    public string LogPath => this.logPath;

    public LogBroker(string logPath) =>
        this.logPath = Path.GetFullPath(logPath);

    public async ValueTask ResetAsync() =>
        await File.WriteAllTextAsync(this.logPath, string.Empty);

    public async ValueTask WriteAsync(string content) =>
        await File.AppendAllTextAsync(this.logPath, content + Environment.NewLine);
}
