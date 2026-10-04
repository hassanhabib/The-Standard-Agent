// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Models.Brokers.Mcps;

internal sealed record ToolCallContent(
    string Type,
    string? Text,
    EmbeddedResource? Resource,
    string? Uri,
    string? Name,
    string? MimeType);

internal sealed record EmbeddedResource(
    string? Uri,
    string? Text);
