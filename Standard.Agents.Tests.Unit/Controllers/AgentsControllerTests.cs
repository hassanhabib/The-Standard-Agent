// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Standard.Agents.Host.Controllers;
using Standard.Agents.Host.Models;
using Standard.Agents.Models.Clients.Agents;
using Standard.Agents.Models.Orchestrations.Agents;
using Xunit;

namespace Standard.Agents.Tests.Unit.Controllers;

// An exposer is pure mapping over one service dependency, so its tests are mapping tests:
// success in, protocol out; invalid in, 400 out; nothing else in between.
public partial class AgentsControllerTests
{
    private readonly Mock<IAgent> agentMock;
    private readonly AgentsController agentsController;

    public AgentsControllerTests()
    {
        this.agentMock = new Mock<IAgent>();

        this.agentsController = new AgentsController(this.agentMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    public async Task ShouldPostRunAsync()
    {
        // given
        var request = new AgentRunRequest(Prompt: "what is owed");

        this.agentMock.Setup(agent =>
            agent.RunAsync("what is owed", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AgentOutcome("the answer", AgentStatus.Responded));

        // when
        ActionResult<AgentRunResponse> actualResult =
            await this.agentsController.PostRunAsync(request);

        // then
        OkObjectResult okResult = actualResult.Result.Should().BeOfType<OkObjectResult>().Subject;

        okResult.Value.Should().BeEquivalentTo(
            new AgentRunResponse(Result: "the answer", Status: "Responded"));
    }
}
