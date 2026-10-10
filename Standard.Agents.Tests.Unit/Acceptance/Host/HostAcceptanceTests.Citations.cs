// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Standard.Agents.Host.Models.V1;
using Standard.Agents.Models.Foundations.Knowledges;
using Standard.Agents.Models.Orchestrations.Agents;
using Xunit;

namespace Standard.Agents.Tests.Unit.Acceptance.Host;

// SPEC.md §4.2 across HTTP: 6.0.0 let a request ask for citation, and the V1 run request had no
// field to ask with, so a caller of a deployment that expressed no opinion could never get the
// sources. The real agent stands behind the door here, because the rule being pinned - what the
// deployment configured wins over what the request asks - lives in the agent, not the host.
public partial class HostAcceptanceTests
{
    private const string RefundsSource = "Refund Policy (2026)";
    private const string RefundsAnswer = "Enterprise customers may request a refund within 90 days.";

    private static IReadOnlyList<KnowledgeResult> RefundsGrounding() =>
    [
        new KnowledgeResult
        {
            Text = "Refund policy. Enterprise customers may request a refund within 90 days.",
            Score = 0.91,
            Source = RefundsSource
        }
    ];

    private WebApplicationFactory<Program> CreateGroundedHost(
        Func<StandardAgent, StandardAgent> configure)
    {
        IReadOnlyList<KnowledgeResult> knowledgeResults = RefundsGrounding();

        StandardAgent groundedAgent = new StandardAgent()
            .WithoutMemory()
            .OnSourcedKnowledge(async query => knowledgeResults)
            .OnBrain(async (systemPrompt, userPrompt) => $"FINAL: {RefundsAnswer}");

        IAgent configuredAgent = configure(groundedAgent);

        return this.hostFactory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddSingleton(configuredAgent)));
    }

    private static async ValueTask<AgentRunResponseV1?> PostCitationRunAsync(
        WebApplicationFactory<Program> host,
        bool? citeKnowledge)
    {
        var request = new AgentRunRequestV1
        {
            Prompt = "what is the refund window?",
            CiteKnowledge = citeKnowledge
        };

        using HttpClient client = host.CreateClient();

        using HttpResponseMessage response =
            await client.PostAsJsonAsync("api/V1/agents/runs", request, wireOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return await response.Content.ReadFromJsonAsync<AgentRunResponseV1>(wireOptions);
    }

    [Fact]
    public async Task ShouldCiteTheSourcesWhenTheRequestAsksThroughHttpAsync()
    {
        // given — a deployment that expressed no opinion, and a caller who asks
        using WebApplicationFactory<Program> host = CreateGroundedHost(agent => agent);
        string expectedResult = $"{RefundsAnswer}\n\nSource: {RefundsSource}";

        // when
        AgentRunResponseV1? actualResponse = await PostCitationRunAsync(host, citeKnowledge: true);

        // then
        actualResponse!.Result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task ShouldCiteTheSourcesWhenTheDeploymentCitesAndTheRequestDeclinesThroughHttpAsync()
    {
        // given — a deployment that must cite cannot be switched off by a caller
        using WebApplicationFactory<Program> host = CreateGroundedHost(agent => agent.CiteKnowledge());
        string expectedResult = $"{RefundsAnswer}\n\nSource: {RefundsSource}";

        // when
        AgentRunResponseV1? actualResponse = await PostCitationRunAsync(host, citeKnowledge: false);

        // then
        actualResponse!.Result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task ShouldNotCiteTheSourcesWhenTheDeploymentDeclinesAndTheRequestAsksThroughHttpAsync()
    {
        // given — a deployment that never cites cannot be switched on by a caller
        using WebApplicationFactory<Program> host =
            CreateGroundedHost(agent => agent.CiteKnowledge(cite: false));

        // when
        AgentRunResponseV1? actualResponse = await PostCitationRunAsync(host, citeKnowledge: true);

        // then
        actualResponse!.Result.Should().Be(RefundsAnswer);
    }
}
