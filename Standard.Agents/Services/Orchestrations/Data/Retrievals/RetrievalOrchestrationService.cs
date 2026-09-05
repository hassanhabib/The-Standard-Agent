// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Models.Brokers.Mcps;
using Standard.Agents.Models.Foundations.ExternalTools.Exceptions;
using Standard.Agents.Services.Foundations.ExternalTools;
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

    // Remote tools are a foundation this orchestration depends on like any other — visible at
    // the tier a dependency is reviewed at, with its failures localized and categorized there.
    // They used to arrive as a model carrying a delegate, which evaded that tier and reported an
    // outage as "no tools" (principal review 2026-09-04, F-15).
    private readonly IExternalToolService externalToolService;

    // The catalog per tool (name → its rendered line), so a run under selection (SPEC.md
    // §4.15) can be shown only what it was offered. Null keeps the whole-string catalog the
    // only source — the pre-selection behavior, byte for byte.
    private readonly IReadOnlyDictionary<string, string>? toolCatalogEntries;

    // The remote tools, kept after the first successful discovery so a healthy server is asked
    // once per composition rather than once per prompt.
    private IReadOnlyList<McpTool>? remoteTools;

    public RetrievalOrchestrationService(
        ISkillService skillService,
        IKnowledgeService knowledgeService,
        string toolCatalog,
        ILoggingBroker loggingBroker,
        IExternalToolService externalToolService,
        IReadOnlyDictionary<string, string>? toolCatalogEntries = null)
    {
        this.skillService = skillService;
        this.knowledgeService = knowledgeService;
        this.toolCatalog = toolCatalog;
        this.loggingBroker = loggingBroker;
        this.externalToolService = externalToolService;
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
        if (skills.Contains(ToolsMarker) is false)
        {
            return SelectedToolCatalog();
        }

        string localCatalog = SelectedToolCatalog();
        string remoteCatalog = RenderRemoteToolCatalog(await DiscoverRemoteToolsAsync());

        if (string.IsNullOrEmpty(remoteCatalog))
        {
            return localCatalog;
        }

        return string.IsNullOrEmpty(localCatalog)
            ? remoteCatalog
            : $"{localCatalog}\n{remoteCatalog}";
    }

    // Best-effort and cached on success: a server down at discovery hides only its own tools
    // this turn and is asked again on the next. The foundation has already localized and logged
    // its failure at the severity it earned, so this tier degrades to no remote tools rather
    // than reporting the outage as an empty server or failing the run.
    private async ValueTask<IReadOnlyList<McpTool>> DiscoverRemoteToolsAsync()
    {
        if (this.remoteTools is not null)
        {
            return this.remoteTools;
        }

        try
        {
            this.remoteTools = await this.externalToolService.RetrieveToolsAsync();

            return this.remoteTools;
        }
        catch (ExternalToolDependencyException)
        {
            return [];
        }
        catch (ExternalToolDependencyValidationException)
        {
            return [];
        }
        catch (ExternalToolServiceException)
        {
            return [];
        }
    }

    // The same rules the local catalog lives by: a description is the opt-in (SPEC.md §6.1), so
    // an undescribed remote tool stays callable but unlisted; a run under selection is shown
    // only what it was offered (SPEC.md §4.15); and what the tool takes is part of the
    // advertisement, so the schema the server declared rides the line as a local tool's
    // Parameters do.
    private static string RenderRemoteToolCatalog(IReadOnlyList<McpTool> remoteTools)
    {
        IReadOnlyList<string>? offered = Models.Loggings.AgentRun.Current?.OfferedTools;

        IEnumerable<string> catalogLines = remoteTools
            .Where(tool => string.IsNullOrWhiteSpace(tool.Description) is false)
            .Where(tool => offered is null
                || offered.Contains(tool.Name, StringComparer.OrdinalIgnoreCase))
            .Select(tool =>
                $"- {tool.Name} — {tool.Description} parameters: {tool.InputSchemaJson}");

        return string.Join("\n", catalogLines);
    }

    // Every tool the servers declared, described or not, so the callers apply the opt-in
    // themselves — selection judges described names, the native list advertises described
    // definitions, and the perimeter binds described names (principal review 2026-09-04, F-04).
    public ValueTask<IReadOnlyList<McpTool>> RetrieveRemoteToolsAsync() =>
    TryCatch(async () => await DiscoverRemoteToolsAsync());

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
