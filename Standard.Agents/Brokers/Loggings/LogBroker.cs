// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Brokers.Loggings;

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
