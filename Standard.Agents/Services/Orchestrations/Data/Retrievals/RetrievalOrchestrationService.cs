// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Models.Orchestrations.Retrievals;
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

    // A model carrying a delegate rather than a broker, because remote tools are Direction's
    // resource and this is Data's tier — configuration crosses natures where a dependency may
    // not. Null when there is nothing remote to advertise.
    private readonly ExternalToolCatalog? externalToolCatalog;

    // The catalog per tool (name → its rendered line), so a run under selection (SPEC.md
    // §4.15) can be shown only what it was offered. Null keeps the whole-string catalog the
    // only source — the pre-selection behavior, byte for byte.
    private readonly IReadOnlyDictionary<string, string>? toolCatalogEntries;

    public RetrievalOrchestrationService(
        ISkillService skillService,
        IKnowledgeService knowledgeService,
        string toolCatalog,
        ILoggingBroker loggingBroker,
        ExternalToolCatalog? externalToolCatalog = null,
        IReadOnlyDictionary<string, string>? toolCatalogEntries = null)
    {
        this.skillService = skillService;
        this.knowledgeService = knowledgeService;
        this.toolCatalog = toolCatalog;
        this.loggingBroker = loggingBroker;
        this.externalToolCatalog = externalToolCatalog;
        this.toolCatalogEntries = toolCatalogEntries;
    }

    // The catalogs are expanded here rather than in a skill file, because which tools a Brain may
    // reach for is a safety boundary and the marker is the developer's opt-in (SPEC.md §6.1).
    public ValueTask<string> RetrieveInstructionsAsync(string route) =>
    TryCatch(async () =>
    {
        string skills = await this.skillService.RetrieveSkillsAsync(route);
        string instructions = skills.Replace(ToolsMarker, await RenderToolCatalogAsync(skills));

        if (instructions.Contains(SkillsMarker))
        {
            string skillCatalog = await this.skillService.RetrieveSkillCatalogAsync();
            instructions = instructions.Replace(SkillsMarker, skillCatalog);
        }

        return instructions;
    });

    // Selection (SPEC.md §4.15): a run under selection is shown only the offered tools' lines.
    // Read off the ambient run for the same reason the run's identity is — two tiers below the
    // loop, and no renderer should gain a parameter for it. Absent a selection (or absent the
    // per-tool entries an older composition never passed), the whole catalog renders unchanged.
    private string SelectedToolCatalog()
    {
        if (Models.Loggings.AgentRun.Current?.OfferedTools is not { } offered
            || this.toolCatalogEntries is null)
        {
            return this.toolCatalog;
        }

        return string.Join("\n", this.toolCatalogEntries
            .Where(entry => offered.Contains(entry.Key, StringComparer.OrdinalIgnoreCase))
            .Select(entry => entry.Value));
    }

    // Remote tools join the catalog under the same opt-in as local ones — and only when the
    // marker is present, so an agent that never advertises never pays a discovery call.
    private async ValueTask<string> RenderToolCatalogAsync(string skills)
    {
        if (this.externalToolCatalog is null || skills.Contains(ToolsMarker) is false)
        {
            return SelectedToolCatalog();
        }

        string externalCatalog = await this.externalToolCatalog.DiscoverAsync();
        string localCatalog = SelectedToolCatalog();

        if (string.IsNullOrEmpty(externalCatalog))
        {
            return localCatalog;
        }

        return string.IsNullOrEmpty(localCatalog)
            ? externalCatalog
            : $"{localCatalog}\n{externalCatalog}";
    }

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
