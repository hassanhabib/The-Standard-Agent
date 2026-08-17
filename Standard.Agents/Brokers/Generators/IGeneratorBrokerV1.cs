// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Brokers.Generators.V1;

namespace Standard.Agents.Brokers.Generators;

/// <summary>
/// The V1 brain contract: a conversation in, a structured choice out.
/// </summary>
/// <remarks>
/// A sibling of <see cref="IGeneratorBroker"/>, not a replacement (plan §1.3). V0 stays alive and
/// supported, because the five provider packages implement it and none of them should have to
/// move on our schedule.
/// </remarks>
public interface IGeneratorBrokerV1
{
    ValueTask<GenerationResult> GenerateAsync(
        IReadOnlyList<ConversationMessage> messages,
        IReadOnlyList<ToolDefinition> tools);
}
