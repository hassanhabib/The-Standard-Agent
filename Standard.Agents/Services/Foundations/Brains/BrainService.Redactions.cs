// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Text;
using Standard.Agents.Brokers.Redactions;

namespace Standard.Agents.Services.Foundations.Brains;

public partial class BrainService
{
    // Streaming makes rehydration a boundary problem: a placeholder can arrive split across
    // two tokens, so anything from the last unmatched "{{" onward is held back until more
    // text arrives. Without it a token boundary mid-placeholder would leak "{{EMAIL_" to the
    // caller and never restore the value.
    //
    // The redaction itself is the broker's (SPEC.md §4.6) — this is only the buffering that
    // streaming adds on top.
    private static string DrainReady(
        StringBuilder pending,
        IReadOnlyDictionary<string, string> vault,
        IRedactionBroker redactionBroker)
    {
        string buffer = redactionBroker.Rehydrate(pending.ToString(), vault);

        int hold = buffer.LastIndexOf('{');

        while (hold > 0 && buffer[hold - 1] == '{')
        {
            hold--;
        }

        string ready = hold >= 0 ? buffer[..hold] : buffer;
        string keep = hold >= 0 ? buffer[hold..] : string.Empty;

        pending.Clear();
        pending.Append(keep);

        return ready;
    }
}
