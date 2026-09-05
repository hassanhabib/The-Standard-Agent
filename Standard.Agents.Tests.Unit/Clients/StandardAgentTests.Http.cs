// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Net;
using System.Text;
using FluentAssertions;
using Moq;
using Standard.Agents.Brokers.Knowledges;
using Standard.Agents.Brokers.Memorys;
using Standard.Agents.Brokers.Skills;
using Standard.Agents.Models.Foundations.Skills;
using Xunit;

namespace Standard.Agents.Tests.Unit.Clients;

// Found in the 2026-09-04 principal review (F-23): every HTTP broker built its own HttpClient
// and nothing could reach it, so a host could not put its own handler chain — pooled and
// DNS-refreshing connections from IHttpClientFactory, a proxy, a certificate, a resilience
// handler, an observer — under the agent's traffic. Http(...) is the seam: the host supplies
// the handler, every HTTP broker rides it, and ownership is explicit: the handler is the
// host's, the client around it is the broker's and holds nothing of its own.
public class StandardAgentHttpTests
{
    private const string ChatCompletion =
        "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\"FINAL: 4\"}}],"
            + "\"usage\":{\"prompt_tokens\":1,\"completion_tokens\":1}}";

    // The native protocol carries the answer as the content itself; no FINAL: marker.
    private const string NativeChatCompletion =
        "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\"4\"}}],"
            + "\"usage\":{\"prompt_tokens\":1,\"completion_tokens\":1}}";

    private const string EmptyToolList =
        "{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":{\"tools\":[]}}";

    // Records what went out and answers with one scripted body — the test's stand-in for the
    // handler chain a host would supply.
    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly string body;

        public RecordingHandler(string body) =>
            this.body = body;

        public List<HttpRequestMessage> Requests { get; } = [];
        public List<string> Bodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            this.Requests.Add(request);
            this.Bodies.Add(await (request.Content?.ReadAsStringAsync(cancellationToken)
                ?? Task.FromResult(string.Empty)));

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(this.body, Encoding.UTF8, "application/json")
            };
        }
    }

    private static StandardAgent BareShell()
    {
        var skillBroker = new Mock<ISkillBroker>();
        skillBroker.Setup(broker => broker.SelectSkillsAsync()).ReturnsAsync(new List<Skill>());

        var memoryBroker = new Mock<IMemoryBroker>();
        memoryBroker.Setup(broker => broker.SelectMemoriesAsync()).ReturnsAsync([]);

        var knowledgeBroker = new Mock<IKnowledgeBroker>();

        knowledgeBroker.Setup(broker => broker.SelectKnowledgeAsync(It.IsAny<string>()))
            .ReturnsAsync([]);

        return new StandardAgent()
            .UseSkills(skillBroker.Object)
            .UseMemory(memoryBroker.Object)
            .UseKnowledge(knowledgeBroker.Object);
    }

    [Fact]
    public async Task ShouldRouteTheBrainThroughTheSuppliedHttpHandlerAsync()
    {
        // given — a host-owned handler under the text-protocol brain
        var handler = new RecordingHandler(ChatCompletion);

        StandardAgent agent = BareShell()
            .Http(() => handler)
            .Brain("http://brain.test/v1/", "key-1", "model-1");

        // when
        string answer = await agent.ProcessPromptAsync("2+2");

        // then — the request left through the host's handler, at the route the base composes to
        answer.Should().Be("4");
        handler.Requests.Should().ContainSingle();
        handler.Requests[0].RequestUri.Should().Be(new Uri("http://brain.test/v1/chat/completions"));
        handler.Requests[0].Headers.Authorization?.Parameter.Should().Be("key-1");
    }

    [Fact]
    public async Task ShouldRouteTheNativeBrainThroughTheSuppliedHttpHandlerAsync()
    {
        // given — the same handler under the native tool-calling brain, supplied AFTER the brain,
        // because a seam that only works in one order is a trap
        var handler = new RecordingHandler(NativeChatCompletion);

        StandardAgent agent = BareShell()
            .NativeBrain("http://brain.test/v1/", "key-1", "model-1")
            .Http(() => handler);

        // when
        string answer = await agent.ProcessPromptAsync("2+2");

        // then
        answer.Should().Be("4");
        handler.Requests.Should().ContainSingle();
        handler.Requests[0].RequestUri.Should().Be(new Uri("http://brain.test/v1/chat/completions"));
        handler.Requests[0].Headers.Authorization?.Parameter.Should().Be("key-1");
    }

    [Fact]
    public async Task ShouldRouteMcpDiscoveryThroughTheSuppliedHttpHandlerAsync()
    {
        // given — an MCP server registered before the handler, discovered on the run
        var handler = new RecordingHandler(EmptyToolList);

        StandardAgent agent = BareShell()
            .OnBrain(async (_, _) => "FINAL: done")
            .Mcp("http://mcp.test/", apiKey: "mcp-key")
            .Http(() => handler);

        // when
        await agent.ProcessPromptAsync("anything");

        // then — discovery went out through the host's handler, credentials intact
        handler.Requests.Should().ContainSingle();
        handler.Requests[0].RequestUri.Should().Be(new Uri("http://mcp.test/"));
        handler.Requests[0].Headers.GetValues("X-Api-Key").Should().ContainSingle("mcp-key");
        handler.Bodies[0].Should().Contain("tools/list");
    }
}
