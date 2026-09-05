// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Brokers.Mcps;

namespace Standard.Agents.Services.Foundations.ExternalTools;

public interface IExternalToolService
{
    ValueTask<string> CallAsync(string name, string input);

    /// <summary>
    /// The remote tools the configured servers offer, in business language: what the agent may
    /// reach for, localized and categorized like every other foundation answer. A server that
    /// cannot be asked is a dependency failure this service names, never an empty catalog.
    /// </summary>
    ValueTask<IReadOnlyList<McpTool>> RetrieveToolsAsync();
}
