// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Brokers.Times;

public interface ITimeBroker
{
    DateTimeOffset GetCurrentDateTimeOffset();
}
