// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Standard.Agents.Host.Controllers;
using Xunit;

namespace Standard.Agents.Tests.Unit.Controllers;

public class HomeControllerTests
{
    [Fact]
    public void ShouldGetAlive()
    {
        // given
        var homeController = new HomeController();

        // when
        ActionResult<string> actualResult = homeController.Get();

        // then — aliveness and nothing else: no security, no dependencies, no claims
        OkObjectResult okResult = actualResult.Result.Should().BeOfType<OkObjectResult>().Subject;

        okResult.Value.Should().Be("The Standard Agent is alive.");
    }
}
