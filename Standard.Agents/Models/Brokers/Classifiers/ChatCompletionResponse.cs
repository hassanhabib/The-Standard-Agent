// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Models.Brokers.Classifiers;

internal sealed record ChatCompletionResponse(
    IReadOnlyList<ChatChoice> Choices);
