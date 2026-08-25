// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Models.Orchestrations.Retrievals;

/// <summary>
/// How Retrieval learns what remote tools exist, carried as a model because a delegate that
/// shapes what the Brain may reach for is policy, and policy is Data — named here, where its
/// arrival is a reviewed diff, rather than constructor sprawl. Remote tools are Direction's
/// resource; this crosses natures as configuration, which a dependency may not.
/// </summary>
public sealed record ExternalToolCatalog(Func<ValueTask<string>> DiscoverAsync);
