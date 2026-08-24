// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;

namespace Standard.Agents.Host.Controllers;

// The heartbeat: no security, no dependencies, aliveness and nothing else - the endpoint a
// load balancer or a first-day deployment checks before anything harder.
[ApiController]
[Route("api/[controller]")]
public class HomeController : ControllerBase
{
    [HttpGet]
    public ActionResult<string> Get() =>
        Ok("The Standard Agent is alive.");
}
