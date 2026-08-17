// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Services.Foundations.Knowledges;
using Standard.Agents.Services.Foundations.Skills;

namespace Standard.Agents.Services.Orchestrations.Data.Retrievals;

public partial class RetrievalOrchestrationService : IRetrievalOrchestrationService
{
    private const string ToolsMarker = "{{tools}}";
    private const string SkillsMarker = "{{skills}}";

    private readonly ISkillService skillService;
    private readonly IKnowledgeService knowledgeService;
    private readonly string toolCatalog;
    private readonly ILoggingBroker loggingBroker;

    public RetrievalOrchestrationService(
        ISkillService skillService,
        IKnowledgeService knowledgeService,
        string toolCatalog,
        ILoggingBroker loggingBroker)
    {
        this.skillService = skillService;
        this.knowledgeService = knowledgeService;
        this.toolCatalog = toolCatalog;
        this.loggingBroker = loggingBroker;
    }

    // The catalogs are expanded here rather than in a skill file, because which tools a Brain may
    // reach for is a safety boundary and the marker is the developer's opt-in (SPEC.md §6.1).
    public ValueTask<string> RetrieveInstructionsAsync(string route) =>
    TryCatch(async () =>
    {
        string skills = await this.skillService.RetrieveSkillsAsync(route);
        string instructions = skills.Replace(ToolsMarker, this.toolCatalog);

        if (instructions.Contains(SkillsMarker))
        {
            string skillCatalog = await this.skillService.RetrieveSkillCatalogAsync();
            instructions = instructions.Replace(SkillsMarker, skillCatalog);
        }

        return instructions;
    });

    public ValueTask<IReadOnlyList<string>> RetrieveGroundingAsync(string query) =>
    TryCatch(async () =>
    {
        IReadOnlyList<string> knowledge =
            await this.knowledgeService.RetrieveKnowledgeAsync(query);

        await this.loggingBroker.LogProcessAsync(
            "Data", $"Retrieved {knowledge.Count} knowledge matches");

        return knowledge;
    });
}
