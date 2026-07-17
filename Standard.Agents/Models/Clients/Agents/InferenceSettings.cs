// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Models.Clients.Agents;

internal sealed record InferenceSettings(
    string ApiUrl,
    string ApiKey,
    string Model,
    double Temperature,
    int MaxTokens,
    int TimeoutSeconds);
