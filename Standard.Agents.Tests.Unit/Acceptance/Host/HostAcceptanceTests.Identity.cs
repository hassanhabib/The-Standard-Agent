// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Standard.Agents.Host.Models.V1;
using Standard.Agents.Host.Security;
using Standard.Agents.Tools;
using Xunit;

namespace Standard.Agents.Tests.Unit.Acceptance.Host;

public partial class HostAcceptanceTests
{
    // A scheme that believes the Authorization header: "Test <subject>" authenticates as that
    // subject with a tenant claim. It stands in for whatever the deployment configures - JWT
    // bearer from the host's configuration, or a proxy's headers - which is exactly the point:
    // the host hands the agent whoever the scheme established, and nothing else.
    private sealed class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            string header = this.Request.Headers.Authorization.ToString();

            if (header.StartsWith("Test ", StringComparison.Ordinal) is false)
            {
                return AuthenticateResult.NoResult();
            }

            var identity = new ClaimsIdentity(
                [new Claim("sub", header["Test ".Length..]), new Claim("tid", "peerllm")],
                authenticationType: "Test");

            return AuthenticateResult.Success(
                new AuthenticationTicket(new ClaimsPrincipal(identity), "Test"));
        }
    }

    private sealed class WireTool : ITool
    {
        public string Name => "wire";

        public string Description => "Moves money.";

        public async ValueTask<string> ExecuteAsync(string input) => "sent";
    }

    // The real agent behind the door this time, composed the way the host composes it: the
    // principal comes from the host's resolver over the request's authenticated user, and an act
    // that needs approval is held with that principal on the pending effect.
    private WebApplicationFactory<Program> CreateAuthenticatedHost() =>
        this.hostFactory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", _ => { });

            services.AddSingleton<IAgent>(provider =>
                new StandardAgent()
                    .WithoutMemory()
                    .Tool(new WireTool())
                    .RequireApproval("wire")
                    .Principal(provider.GetRequiredService<HttpPrincipalResolver>().Resolve)
                    .OnBrain(async (systemPrompt, userPrompt) => "ACTION: wire: 100"));
        }));

    [Fact]
    public async Task ShouldHandTheAuthenticatedUserToTheAgentAsThePrincipalThroughHttpAsync()
    {
        // given
        using WebApplicationFactory<Program> authenticatedHost = CreateAuthenticatedHost();
        using HttpClient client = authenticatedHost.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "Test hassan");

        // when
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "api/V1/agents/runs",
            new AgentRunRequestV1 { Prompt = "wire the money" },
            wireOptions);

        AgentRunResponseV1? actualResponse =
            await response.Content.ReadFromJsonAsync<AgentRunResponseV1>(wireOptions);

        // then — held for approval, and the act names who asked, as the scheme established it
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        actualResponse!.Status.Should().Be("AwaitingApproval");
        actualResponse.PendingEffect.Should().NotBeNull();
        actualResponse.PendingEffect!.Principal.Should().Be("hassan");
    }

    [Fact]
    public async Task ShouldHandNoPrincipalToTheAgentForAnAnonymousRequestThroughHttpAsync()
    {
        // given — the same host, no credential presented, no authentication configured to require one
        using WebApplicationFactory<Program> authenticatedHost = CreateAuthenticatedHost();
        using HttpClient client = authenticatedHost.CreateClient();

        // when
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "api/V1/agents/runs",
            new AgentRunRequestV1 { Prompt = "wire the money" },
            wireOptions);

        AgentRunResponseV1? actualResponse =
            await response.Content.ReadFromJsonAsync<AgentRunResponseV1>(wireOptions);

        // then — nobody was invented
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        actualResponse!.PendingEffect!.Principal.Should().BeNull();
    }

    [Fact]
    public async Task ShouldRequireAnAuthenticatedUserOnAgentRoutesWhenAuthenticationIsConfiguredAsync()
    {
        // given — a deployment that configured bearer authentication against an issuer
        using WebApplicationFactory<Program> lockedHost = this.hostFactory.WithWebHostBuilder(builder =>
            builder.UseSetting("Host:Authentication:Authority", "https://issuer.test/"));

        using HttpClient client = lockedHost.CreateClient();

        // when — no token at all
        using HttpResponseMessage runResponse = await client.PostAsJsonAsync(
            "api/V1/agents/runs",
            new AgentRunRequestV1 { Prompt = "what is owed" },
            wireOptions);

        using HttpResponseMessage heartbeat = await client.GetAsync("api/home");

        // then — the agent route is 401 before any run starts; the heartbeat stays open
        runResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        heartbeat.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
