// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Runtime.CompilerServices;
#if !NET9_0_OR_GREATER
// System.Threading.Lock arrived in .NET 9. On the net8.0 target a plain object under the
// same lock statements is the identical semantic; the alias keeps one body for both.
using Lock = System.Object;
#endif
using Microsoft.Extensions.Logging.Abstractions;
using Standard.Agents.Brokers.Agents;
using Standard.Agents.Brokers.Audits;
using Standard.Agents.Brokers.Approvals;
using Standard.Agents.Brokers.Classifiers;
using Standard.Agents.Brokers.Contracts;
using Standard.Agents.Brokers.Effects;
using Standard.Agents.Brokers.Policies;
using Standard.Agents.Brokers.Files;
using Standard.Agents.Brokers.Generators;
using Standard.Agents.Brokers.Knowledges;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Brokers.Mcps;
using Standard.Agents.Brokers.Memorys;
using Standard.Agents.Brokers.Redactions;
using Standard.Agents.Brokers.Resiliences;
using Standard.Agents.Brokers.Sessions;
using Standard.Agents.Brokers.Usages;
using Standard.Agents.Brokers.Skills;
using Standard.Agents.Brokers.Telemetries;
using Standard.Agents.Brokers.Times;
using Standard.Agents.Brokers.Tools;
using Standard.Agents.Brokers.Verifiers;
using Standard.Agents.Models.Brokers.Effects;
using Standard.Agents.Models.Brokers.Agents;
using Standard.Agents.Models.Brokers.Audits;
using Standard.Agents.Models.Brokers.Generators;
using Standard.Agents.Models.Brokers.Generators.V1;
using Standard.Agents.Models.Brokers.Sessions;
using Standard.Agents.Models.Clients.Agents;
using Standard.Agents.Models.Coordinations.Agents;
using Standard.Agents.Models.Coordinations.Directions;
using Standard.Agents.Models.Foundations.Brains;
using Standard.Agents.Models.Foundations.Skills;
using Standard.Agents.Models.Orchestrations.Effects;
using Standard.Agents.Models.Loggings;
using Standard.Agents.Prompts;
using Standard.Agents.Services.Managements;
using Standard.Agents.Services.Foundations.Brains;
using Standard.Agents.Services.Foundations.Contracts;
using Standard.Agents.Services.Foundations.ExternalTools;
using Standard.Agents.Services.Foundations.Gates;
using Standard.Agents.Services.Foundations.InternalTools;
using Standard.Agents.Services.Foundations.Judges;
using Standard.Agents.Services.Foundations.Knowledges;
using Standard.Agents.Services.Foundations.Memorys;
using Standard.Agents.Services.Foundations.Returns;
using Standard.Agents.Services.Foundations.Approvals;
using Standard.Agents.Services.Foundations.EffectLedgers;
using Standard.Agents.Services.Foundations.Policys;
using Standard.Agents.Services.Foundations.Sessions;
using Standard.Agents.Services.Foundations.Skills;
using Standard.Agents.Services.Foundations.Usages;
using Standard.Agents.Services.Coordinations.Data;
using Standard.Agents.Services.Orchestrations.Data.Recollections;
using Standard.Agents.Services.Orchestrations.Data.Retrievals;
using Standard.Agents.Services.Coordinations.Decision;
using Standard.Agents.Services.Orchestrations.Decision.Guardians;
using Standard.Agents.Services.Orchestrations.Decision.Inferences;
using Standard.Agents.Services.Coordinations.Direction;
using Standard.Agents.Services.Orchestrations.Direction.Executions;
using Standard.Agents.Services.Orchestrations.Direction.Perimeters;
using Standard.Agents.Tools;

namespace Standard.Agents;

// The composition engine: the one place the configured verbs become a graph of brokers and
// services, cached until a verb changes the configuration (principal review 2026-09-04,
// F-31). Nothing here is a verb; nothing in StandardAgent.cs composes.
public sealed partial class StandardAgent
{
    // Composes once and reuses. Guarded because one agent serves prompts concurrently
    // (SPEC.md §4.4) and an unguarded `??=` lets two arriving prompts each build a graph —
    // two brokers over one audit sink, two of everything, one silently discarded.
    //
    // Registry selection is async (a registry can be a service call away) and a lock cannot
    // hold an await, so resolution is three steps: reuse under the lock, select outside it,
    // compose under it again. A builder call landing between the steps nulls the cache, so
    // the very next resolution selects and composes afresh — nothing configured is lost.
    private async ValueTask<IRunManagementService> ResolveAgentAsync()
    {
        IAgentRegistryBroker[] registries;

        lock (this.compositionLock)
        {
            if (this.agent is not null)
            {
                return this.agent;
            }

            registries = [.. this.agentSources];
        }

        List<RegisteredAgent> registeredAgents = [];

        foreach (IAgentRegistryBroker registry in registries)
        {
            registeredAgents.AddRange(await registry.SelectAgentsAsync());
        }

        lock (this.compositionLock)
        {
            return this.agent ??= Compose(registeredAgents);
        }
    }

    // Every builder method drops the cached composition, so configuration set after a
    // prompt still takes effect. Returning `this` without doing that would silently
    // ignore the change.
    /// <summary>
    /// Puts the host's HTTP handler chain under every HTTP broker the agent composes: the brain,
    /// the native brain, and MCP servers. The <b>External</b> seam for HTTP itself — pooled and
    /// DNS-refreshing connections from <c>IHttpClientFactory</c>
    /// (<c>IHttpMessageHandlerFactory.CreateHandler</c>), a proxy, a certificate, a resilience
    /// handler, an observer — where a broker that built its own client could reach none of them
    /// (principal review 2026-09-04, F-23).
    /// </summary>
    /// <remarks>
    /// Ownership is explicit. A handler this delegate returns is yours: the broker wraps it in a
    /// client that holds nothing of its own and never disposes it. Without this call every HTTP
    /// broker creates its own handler, owned by the composition and released on the next one, so
    /// a single long-lived agent holds one set for its life; short-lived agents composed by the
    /// dozen should supply handlers from a factory instead. Order does not matter: brokers are
    /// created at composition, so the handler reaches a brain or server registered before it.
    /// </remarks>
    /// <param name="handlers">Returns a handler for one broker; called once per HTTP broker.</param>
    /// <returns>The same agent, so calls can be chained.</returns>
    public StandardAgent Http(Func<HttpMessageHandler> handlers) =>
        Set(() => this.httpHandlerSource = handlers);

    private StandardAgent Set(Action configure)
    {
        lock (this.compositionLock)
        {
            configure();
            this.agent = null;
        }

        return this;
    }

    // Reads an optional prompt file (constitution, consumption) resolved against the build
    // output like skills. A missing file yields empty rather than an error, so a stale path
    // degrades to the built-in guardian policy instead of bricking composition.
    private static string ReadOptionalFile(IFileBroker file, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        string fullPath = Path.Combine(AppContext.BaseDirectory, path);

        return file.FileExists(fullPath)
            ? file.ReadFile(fullPath)
            : string.Empty;
    }

    // Assembles a guardian rubric from its parts in order, skipping any that are empty:
    // constitution first (the law), then the policy (what to screen or score), then the
    // contract (the output protocol the broker parses). The contract stays last so it is
    // never displaced, whatever the policy above it.
    private static string ComposeGuardianRubric(params string[] parts) =>
        string.Join("\n\n", parts.Where(part => string.IsNullOrWhiteSpace(part) is false));

    // One handler per HTTP broker: the host's when Http(...) was called, otherwise one this
    // composition owns and the next composition releases.
    private HttpMessageHandler CreateHttpHandler()
    {
        if (this.httpHandlerSource is not null)
        {
            return this.httpHandlerSource();
        }

        var handler = new HttpClientHandler();
        this.ownedHttpHandlers.Add(handler);

        return handler;
    }

    private void ReleaseOwnedHttpHandlers()
    {
        foreach (HttpMessageHandler handler in this.ownedHttpHandlers)
        {
            handler.Dispose();
        }

        this.ownedHttpHandlers.Clear();
    }

    private IRunManagementService Compose(IReadOnlyList<RegisteredAgent> registeredAgents)
    {
        ValidateComposition();
        ReleaseOwnedHttpHandlers();

        InferenceSettings? brain = this.brainSettings;

        IFileBroker file = new FileBroker();

        // The log is one broker; the decision log is another, applied over it by decoration
        // when an audit sink is configured - over the built-in trace or a host's own logging
        // broker alike, so .UseLogging(...) and .Audit(...) compose rather than compete (F-16).
        ILoggingBroker trace =
            this.loggingBroker ?? new LoggingBroker(
                new NullLogger<LoggingBroker>(),
                this.traceVerbosity,
                string.IsNullOrEmpty(this.logPath) ? null : this.logPath);

        IAuditBroker? audit =
            this.auditBroker
                ?? (string.IsNullOrEmpty(this.auditPath) ? null : new FileAuditBroker(this.auditPath));

        // Composed here, above the log, because the same redaction that guards the model
        // boundary guards the audit boundary (F-14); the generators and guardians below take it
        // by decoration as before.
        IRedactionBroker redaction = this.redactionBroker
            ?? (this.redactionRules is null
                ? new NotConfiguredRedactionBroker()
                : new RuleRedactionBroker(this.redactionRules));

        ILoggingBroker logging = audit is null
            ? trace
            : new AuditingLoggingBroker(
                trace,
                audit,
                new TimeBroker(),
                redaction,
                new AuditPolicy(Payloads: this.auditPayloads),
                () => this.identityResolver?.Invoke()?.Id);

        // With only a native brain configured there is no V0 generator to build, and none is
        // needed: SpeaksNatively routes every call to the V1 seam. The placeholder exists so the
        // Brain foundation keeps one shape rather than growing a nullable dependency.
        IGeneratorBroker generator =
            this.generatorBroker
                ?? (brain is null
                    ? new FunctionGeneratorBroker((systemPrompt, userPrompt) =>
                        throw new InvalidOperationException(
                            "This agent has a native brain; the text protocol is not in use."))
                    // The legacy-path values, resolved configured → framework default at
                    // composition. A per-request value arrives on the request-carrying overload
                    // and never through here.
                    : new GeneratorBroker(
                        CreateHttpHandler(),
                        brain.ApiUrl, brain.ApiKey, brain.Model,
                        brain.Temperature ?? ResolvedInference.DefaultTemperature,
                        brain.MaxTokens ?? ResolvedInference.DefaultMaxTokens,
                        brain.TimeoutSeconds));

        // Without memory there is nothing to recall and no way to store: the foundation reads a
        // not-configured broker, and the remember tool is never registered, so the model is not
        // offered it (principal review 2026-09-04, F-05).
        IMemoryService memoryService = this.memoryDisabled
            ? new MemoryService(new NotConfiguredMemoryBroker(), logging)
            : this.memoryBroker is null
                ? new MemoryService(file, Path.GetFullPath(this.memoryPath), logging)
                : new MemoryService(this.memoryBroker, logging);

        List<ITool> allTools = this.memoryDisabled
            ? [.. this.tools]
            : [.. this.tools, new RememberTool(memoryService.RememberAsync)];

        // The fleet materializes as tools — which is the whole design: a handoff is an act, so
        // the advertisement opt-in, the perimeter, the audit and cancellation across the seam
        // all apply because they already applied to tools. First to claim a name keeps it, the
        // rule MCP servers live by, so a registry cannot shadow a tool the host wired in code.
        HashSet<string> claimedNames =
            new(allTools.Select(tool => tool.Name), StringComparer.OrdinalIgnoreCase);

        foreach (RegisteredAgent registered in registeredAgents)
        {
            if (claimedNames.Add(registered.Name) is false)
            {
                continue;
            }

            allTools.Add(new AgentTool(
                registered.Name,
                registered.Agent,
                AgentTool.GroundedHandoff,
                registered.Description));
        }

        string constitution = ReadOptionalFile(file, this.constitutionPath);
        string consumption = ReadOptionalFile(file, this.consumptionPath);

        string gatePolicy = string.IsNullOrWhiteSpace(consumption)
            ? GuardianPrompts.GatePolicy
            : consumption;

        string judgePolicy = string.IsNullOrWhiteSpace(consumption)
            ? GuardianPrompts.JudgePolicy
            : consumption;

        string gateRubric = ComposeGuardianRubric(
            constitution, gatePolicy, GuardianPrompts.GateContract);

        string judgeRubric = ComposeGuardianRubric(
            constitution, judgePolicy, GuardianPrompts.JudgeContract);

        IClassifierBroker classifier =
            this.classifierBroker
                ?? (this.localGateScreen is not null
                    ? new FunctionClassifierBroker(this.localGateScreen, gateRubric)
                    : this.gateSettings is null
                        ? new NotConfiguredClassifierBroker()
                        : new ClassifierBroker(
                            this.gateSettings.ApiUrl, this.gateSettings.ApiKey, this.gateSettings.Model,
                            this.gateSettings.Temperature ?? 0.0, this.gateSettings.MaxTokens ?? 16,
                            this.gateSettings.TimeoutSeconds, gateRubric));

        IVerifierBroker verifier =
            this.verifierBroker
                ?? (this.localJudgeEvaluate is not null
                    ? new FunctionVerifierBroker(this.localJudgeEvaluate, judgeRubric)
                    : this.judgeSettings is null
                        ? new NotConfiguredVerifierBroker()
                        : new VerifierBroker(
                            this.judgeSettings.ApiUrl, this.judgeSettings.ApiKey, this.judgeSettings.Model,
                            this.judgeSettings.Temperature ?? 0.0, this.judgeSettings.MaxTokens ?? 16,
                            this.judgeSettings.TimeoutSeconds, judgeRubric));

        IToolBroker toolBroker = new ToolBroker(allTools);

        // One source composes as itself; several compose behind the same seam the service
        // already speaks — the tier above never learns how many integrations answered.
        List<IMcpBroker> mcpBrokers = [.. this.mcpSources.Select(source => source())];

        IMcpBroker mcp = mcpBrokers.Count switch
        {
            0 => new NotConfiguredMcpBroker(),
            1 => mcpBrokers[0],
            _ => new CompositeMcpBroker(mcpBrokers)
        };

        ISkillBroker skills = this.skillSources.Count switch
        {
            0 => new FileSkillBroker(Path.Combine(AppContext.BaseDirectory, "Skills")),
            1 => this.skillSources[0],
            _ => new CompositeSkillBroker(this.skillSources)
        };

        ISkillService skillService = new SkillService(skills, logging);

        IKnowledgeService knowledgeService = this.knowledgeBroker is null
            ? new KnowledgeService(
                file,
                Path.GetFullPath(this.knowledgePath),
                this.knowledgePattern,
                this.knowledgeMaxResults,
                logging,
                this.knowledgeMinScore)
            : new KnowledgeService(this.knowledgeBroker, logging);

        // One foundation over the remote tools, shared by the two natures that need it: Data
        // advertises what the servers offer, Direction performs what the Brain chose. A single
        // instance keeps discovery and execution reading the same servers.
        IExternalToolService externalToolService = new ExternalToolService(mcp, logging);

        // The Data nature, as two regions and the coordination that composes them. Retrieval is
        // authored material selected by relevance; Recollection is what the agent accumulated.
        DataCoordinationService data = new(
            new RetrievalOrchestrationService(
                skillService, knowledgeService, RenderToolCatalog(allTools), logging,
                externalToolService,
                RenderToolCatalogEntries(allTools)),
            new RecollectionOrchestrationService(
                memoryService,
                new SessionService(
                    this.sessionBroker ?? new NotConfiguredSessionBroker(), logging),
                logging),
            logging);

        // One redaction broker across every model the agent drives. SPEC.md §4.6: the Gate
        // screens the raw task and the Judge reads the task and the draft, and either may run on
        // a different host than the Brain, so redacting only the Brain narrows nothing.
        //
        // It is applied by DECORATING each model broker rather than by handing it to each
        // service. That is what makes "every model call" structural: a foundation holds one
        // broker, knows nothing of redaction, and a fourth model call added tomorrow cannot
        // forget (docs/architecture-alignment.md).
        IResilienceBroker resilience =
            this.resilienceBroker ?? new NotConfiguredResilienceBroker();

        // Retry inside, redaction outside: the redacted prompt is what gets retried, so a value
        // is tokenized once rather than once per attempt, and rehydration happens once at the end.
        IGeneratorBroker generatorAtTheWire = new RedactingGeneratorBroker(
            new RetryingGeneratorBroker(generator, resilience),
            redaction);

        IGeneratorBrokerV1? nativeBrain =
            this.generatorBrokerV1 ?? this.nativeBrainSource?.Invoke();

        IGeneratorBrokerV1? nativeAtTheWire = nativeBrain is null
            ? null
            : new RedactingGeneratorBrokerV1(
                new RetryingGeneratorBrokerV1(nativeBrain, resilience),
                redaction);

        GateService gate = new(new RedactingClassifierBroker(classifier, redaction), logging);

        // The Decision nature, as two regions and the judgment between them. Inference asks the
        // model and reads its answer; Guardian screens what goes in and scores what comes out.
        DecisionCoordinationService decision = new(
            new InferenceOrchestrationService(
                new BrainService(generatorAtTheWire, logging, nativeAtTheWire),
                new UsageService(this.usageBroker, logging),
                logging,
                RenderToolDefinitions(allTools)),
            new GuardianOrchestrationService(
                gate,
                new JudgeService(new RedactingVerifierBroker(verifier, redaction), logging),
                new ContractService(this.contractBroker ?? new RuleContractBroker(), logging),
                logging),
            logging,
            this.contractSchema);

        // The allow-list is expressed as a policy, so the simple answer and an external policy
        // engine travel one seam and a denial carries a reason either way (SPEC.md §4.9).
        IPolicyBroker policy = this.policyBroker
            ?? (this.allowedTools is null
                ? new NotConfiguredPolicyBroker()
                : new AllowListPolicyBroker(this.allowedTools));

        IApprovalBroker approvals = this.approvalBroker
            ?? (this.approvalRequiredTools is null
                ? new NotConfiguredApprovalBroker()
                : new RequireApprovalBroker(this.approvalRequiredTools));

        // The Direction nature, as two regions and the order between them. Perimeter answers
        // whether an act may happen; Execution performs it; the sequence belongs to neither.
        DirectionCoordinationService direction = new(
            new PerimeterOrchestrationService(
                new PolicyService(policy, logging),
                new ApprovalService(approvals, logging),
                new EffectLedgerService(
                    this.effectLedgerBroker ?? new InMemoryEffectLedgerBroker(),
                    new TimeBroker(),
                    logging),
                new TimeBroker(),
                logging),
            new ExecutionOrchestrationService(
                new InternalToolService(toolBroker, logging),
                externalToolService,
                new ReturnService(logging),
                logging),
            logging,
            new PerimeterPolicy
            {
                Mode = this.permissionMode,
                IrreversibleTools = [.. this.approvalRequiredTools ?? []],

                DeclaredRisk = this.declaredRisk
                    ?? new Dictionary<string, RiskLevel>(StringComparer.OrdinalIgnoreCase),

                // What each tool says about itself, read once at composition. The tool is the
                // only thing that knows what its arguments mean, and the framework never
                // parses them.
                ToolRisk = allTools.ToDictionary(
                    tool => tool.Name,
                    tool => tool.Risk,
                    StringComparer.OrdinalIgnoreCase),

                ToolScope = allTools.ToDictionary(
                    tool => tool.Name,
                    tool => (Func<string, string>)tool.ScopeOf,
                    StringComparer.OrdinalIgnoreCase),

                // Whether the allow-list speaks to an act at all — which the mode needs and a
                // yes/no authorization decision cannot carry. Null when no allow-list was
                // configured, so Ask asks about everything, which is what it says on the tin.
                ExplicitlyPermits =
                    policy is AllowListPolicyBroker allowList ? allowList.Mentions : null,

                IdentityResolver = this.identityResolver,

                EnforceSelection = this.enforceSelection,

                // What selection could have offered, from the same rendering the catalogs use
                // (SPEC.md §6.1: a description is the opt-in), so the perimeter can tell a
                // withheld tool from an undescribed one and the two can never disagree.
                AdvertisedTools = [.. Advertised(allTools).Select(tool => tool.Name)]
            });

        return new RunManagementService(
            data, decision, direction, logging, this.maxTurns, new TimeBroker(), this.budget,
            this.maxHistoryTurns, this.compensateOnFailure, this.screenToolOutput,
            this.telemetryBroker, this.contractSchema, brain?.Temperature, brain?.MaxTokens,
            allTools.Select(tool => tool.Name),
            RenderToolNarrations(allTools),
            this.localToolSelector is null ? null : new ToolSelector(this.localToolSelector),
            Advertised(allTools).Select(tool => tool.Name),
            this.identityResolver is null ? null : new PrincipalResolver(this.identityResolver));
    }

    // The catalog a "{{tools}}" marker in the agent's Data expands into. Only tools that
    // carry a description are listed — a description is the opt-in (SPEC 6.1); a tool with
    // none is callable but not advertised. Derived from the registered tools, so it never
    // drifts from what is actually there.
    // The same opt-in rule the text catalog uses (SPEC.md §6.1): a description is what offers a
    // tool to the model. A tool without one stays callable but unadvertised, so declaring tools
    // as data does not quietly widen what the Brain may reach for.
    private static IReadOnlyList<ToolDefinition> RenderToolDefinitions(IEnumerable<ITool> tools) =>
        [.. Advertised(tools)
            .Select(tool => new ToolDefinition(tool.Name, tool.Description, tool.Parameters))];

    // Narration templates, derived from the tools exactly as risk and scope are — the tool is
    // what knows what its act means in the user's language, and derived data cannot drift.
    private static IReadOnlyDictionary<string, ToolNarration> RenderToolNarrations(
        IEnumerable<ITool> tools) =>
        tools
            .Where(tool =>
                string.IsNullOrWhiteSpace(tool.NarrationStarting) is false
                    || string.IsNullOrWhiteSpace(tool.NarrationObserved) is false)
            .ToDictionary(
                tool => tool.Name,
                tool => new ToolNarration(tool.NarrationStarting, tool.NarrationObserved),
                StringComparer.OrdinalIgnoreCase);

    private static string RenderToolCatalog(IEnumerable<ITool> tools)
    {
        IEnumerable<string> describedTools = Advertised(tools)
            .Select(tool => $"- {tool.Name} — {tool.Description} parameters: {tool.Parameters}");

        return string.Join("\n", describedTools);
    }

    // The same catalog, per tool, so a run under selection (SPEC.md §4.15) can be shown only
    // what it was offered. Derived from the same rendering as the whole-string catalog, so the
    // two can never disagree about a tool's line.
    private static IReadOnlyDictionary<string, string> RenderToolCatalogEntries(
        IEnumerable<ITool> tools) =>
        Advertised(tools).ToDictionary(
            tool => tool.Name,
            tool => $"- {tool.Name} — {tool.Description} parameters: {tool.Parameters}",
            StringComparer.OrdinalIgnoreCase);

    // What the model may be told about, in ONE place. The rule (SPEC.md §6.1: a description is
    // the opt-in, and a tool without one stays callable but unadvertised) was written out twice —
    // once for the text catalog Data renders into the prompt, once for the definitions Decision
    // hands the native brain. Two copies of a rule about what the model is allowed to see is two
    // chances to widen it by half, and a model told about a tool in the prompt but not in the
    // schema — or the reverse — behaves differently on each protocol for no stated reason.
    private static IEnumerable<ITool> Advertised(IEnumerable<ITool> tools) =>
        tools.Where(tool => string.IsNullOrWhiteSpace(tool.Description) is false);

}
