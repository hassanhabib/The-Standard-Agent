// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Brokers.Loggings;

public interface ILogBroker
{
    string LogPath { get; }

    ValueTask ResetAsync();

    ValueTask WriteAsync(string content);
}
