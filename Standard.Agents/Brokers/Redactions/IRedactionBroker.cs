// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Brokers.Redactions;

public interface IRedactionBroker
{
    string Redact(string text, IDictionary<string, string> vault);

    string Rehydrate(string text, IReadOnlyDictionary<string, string> vault);
}
