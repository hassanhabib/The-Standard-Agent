// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Logs;

namespace Standard.Agents.Conformance;

public sealed class NullLogBroker : ILogBroker
{
    public string LogPath => string.Empty;

    public ValueTask ResetAsync() =>
        ValueTask.CompletedTask;

    public ValueTask WriteAsync(string content) =>
        ValueTask.CompletedTask;
}
