// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Models.Clients.Agents;

// Temperature and MaxTokens are nullable because "configured" must be representable: precedence
// (docs/per-request-inference.md §4.2) has to distinguish a value the host chose from one the
// host never mentioned. The framework default is the resolution's third rung, applied at the
// boundary — the moment these fields defaulted here, "hard configured wins" silently became
// "defaulted wins" and a request's temperature could never take effect.
internal sealed record InferenceSettings(
    string ApiUrl,
    string ApiKey,
    string Model,
    double? Temperature,
    int? MaxTokens,
    int TimeoutSeconds);
