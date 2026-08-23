// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Standard.Agents.Brokers.Approvals;
using Standard.Agents.Models.Orchestrations.Effects;
using Standard.Agents.Tools;
using Xunit;

namespace Standard.Agents.Tests.Unit.Acceptance;

// Permission, as a deployment actually needs to express it.
//
// The perimeter already had the hard half: a human before an irreversible act, held rather than
// failed, the claim released so an approval cannot arrive too late, and the act travelling with
// the pause so an authority can be shown what they are permitting.
//
// What it did not have was granularity. Permission was a list of tool NAMES, one list served two
// meanings, and there was no way to say "ask me about anything you have not been told about" —
// which is the only workable posture for an agent touching files, because the targets cannot be
// enumerated in advance.
public class PermissionTests
{
    private sealed class WriteFileTool : ITool
    {
        public string Name => "write_file";
        public string Description => "Writes text to a path.";
        public int Writes { get; private set; }

        // The tool knows what it does. Risk is declared here rather than inferred from whichever
        // list the host happened to put the name in.
        public RiskLevel Risk => RiskLevel.Sensitive;

        // The tool also knows what it is about to TOUCH. The framework cannot parse arbitrary
        // arguments, and a host should not have to reinvent that parsing in a policy delegate.
        public string ScopeOf(string input) => input.Split(' ')[0];

        public ValueTask<string> ExecuteAsync(string input)
        {
            Writes++;

            return ValueTask.FromResult("written");
        }
    }

    private static StandardAgent AgentActing(ITool tool, params string[] replies)
    {
        int asked = 0;

        return new StandardAgent()
            .Tool(tool)
            .OnBrain((_, _) =>
            {
                string reply = replies[Math.Min(asked, replies.Length - 1)];
                asked++;

                return ValueTask.FromResult(reply);
            })
            .MaxTurns(6);
    }

    // Gap 1. One list drove both risk and approval, so RiskLevel.Sensitive could never be
    // produced — a declared level the framework was incapable of reaching, and a policy branching
    // on it was writing dead code.
    [Fact]
    public async Task ShouldCarryTheRiskTheToolDeclaresAsync()
    {
        // given
        var tool = new WriteFileTool();
        RiskLevel? seenRisk = null;

        StandardAgent agent = AgentActing(tool, "ACTION: write_file: /project/a.txt hello", "FINAL: done")
            .OnPolicy(effect =>
            {
                seenRisk = effect.RiskLevel;

                return ValueTask.FromResult(AuthorizationDecision.Allow());
            });

        // when
        await agent.ProcessPromptAsync("write it");

        // then
        seenRisk.Should().Be(
            RiskLevel.Sensitive,
            because: "the tool declares its own risk, and Sensitive must be reachable at all");
    }

    // Gap 2. Permission was per tool NAME. "May write files" is not "may write files under
    // /project", and nothing in the model could express the difference.
    [Fact]
    public async Task ShouldCarryTheScopeTheToolIsAboutToTouchAsync()
    {
        // given
        var tool = new WriteFileTool();
        string? seenScope = null;

        StandardAgent agent = AgentActing(tool, "ACTION: write_file: /project/a.txt hello", "FINAL: done")
            .OnPolicy(effect =>
            {
                seenScope = effect.Scope;

                return ValueTask.FromResult(AuthorizationDecision.Allow());
            });

        // when
        await agent.ProcessPromptAsync("write it");

        // then
        seenScope.Should().Be("/project/a.txt");
    }

    // Gap 2, Local mode. The allow-list should be able to say WHERE, not only WHAT.
    [Fact]
    public async Task ShouldDenyAnActOutsideAnAllowedScopeAsync()
    {
        // given
        var tool = new WriteFileTool();

        StandardAgent agent = AgentActing(tool, "ACTION: write_file: /etc/passwd hello", "FINAL: done")
            .AllowTools("write_file:/project");

        // when
        await agent.ProcessPromptAsync("write it");

        // then
        tool.Writes.Should().Be(
            0,
            because: "the tool is permitted under /project and the act targeted /etc");
    }

    [Fact]
    public async Task ShouldAllowAnActInsideAnAllowedScopeAsync()
    {
        // given
        var tool = new WriteFileTool();

        StandardAgent agent = AgentActing(tool, "ACTION: write_file: /project/a.txt hello", "FINAL: done")
            .AllowTools("write_file:/project");

        // when
        await agent.ProcessPromptAsync("write it");

        // then
        tool.Writes.Should().Be(1);
    }

    // Gap 3. Everything was enumerated by name, so an unlisted tool was silently Safe and
    // silently permitted. Ask-first is the only workable posture when targets cannot be listed in
    // advance, and it was inexpressible.
    [Fact]
    public async Task ShouldAskBeforeAnActThatWasNeverExplicitlyPermittedAsync()
    {
        // given
        var tool = new WriteFileTool();
        int asked = 0;

        StandardAgent agent = AgentActing(tool, "ACTION: write_file: /project/a.txt hello", "FINAL: done")
            .Permissions(PermissionMode.Ask)
            .OnApproval(_ =>
            {
                asked++;

                return ValueTask.FromResult(ApprovalDecision.Denied);
            });

        // when
        await agent.ProcessPromptAsync("write it");

        // then
        asked.Should().Be(
            1,
            because: "nothing permitted this act explicitly, and the mode says ask about "
                + "anything that was not");

        tool.Writes.Should().Be(0);
    }

    [Fact]
    public async Task ShouldNotAskAboutAnActThatWasExplicitlyPermittedAsync()
    {
        // given
        var tool = new WriteFileTool();
        int asked = 0;

        StandardAgent agent = AgentActing(tool, "ACTION: write_file: /project/a.txt hello", "FINAL: done")
            .Permissions(PermissionMode.Ask)
            .AllowTools("write_file:/project")
            .OnApproval(_ =>
            {
                asked++;

                return ValueTask.FromResult(ApprovalDecision.Approved);
            });

        // when
        await agent.ProcessPromptAsync("write it");

        // then
        asked.Should().Be(0, because: "an explicit permission is the answer to the question");
        tool.Writes.Should().Be(1);
    }

    // Gap 4. A granted approval was forgotten immediately, so a second act on the same target
    // asked again — and an authority asked the identical question twice stops reading it.
    [Fact]
    public async Task ShouldRememberAGrantForTheSameToolAndScopeWithinTheRunAsync()
    {
        // given — two writes to the SAME path, then finish
        var tool = new WriteFileTool();
        int asked = 0;

        StandardAgent agent = AgentActing(
            tool,
            "ACTION: write_file: /project/a.txt first",
            "ACTION: write_file: /project/a.txt second",
            "FINAL: done")
                .RequireApproval("write_file")
                .OnApproval(_ =>
                {
                    asked++;

                    return ValueTask.FromResult(ApprovalDecision.Approved);
                });

        // when
        await agent.ProcessPromptAsync("write it twice");

        // then
        asked.Should().Be(
            1,
            because: "the grant was for this tool at this scope, and asking an authority the "
                + "identical question twice is how they stop reading it");

        tool.Writes.Should().Be(2);
    }

    // The other half of gap 4, and the one that makes it safe: a grant is for the scope it was
    // given for. Approving a write to one file is not approving writes to every file.
    [Fact]
    public async Task ShouldAskAgainForADifferentScopeAsync()
    {
        // given — two writes to DIFFERENT paths
        var tool = new WriteFileTool();
        int asked = 0;

        StandardAgent agent = AgentActing(
            tool,
            "ACTION: write_file: /project/a.txt first",
            "ACTION: write_file: /project/b.txt second",
            "FINAL: done")
                .RequireApproval("write_file")
                .OnApproval(_ =>
                {
                    asked++;

                    return ValueTask.FromResult(ApprovalDecision.Approved);
                });

        // when
        await agent.ProcessPromptAsync("write two files");

        // then
        asked.Should().Be(
            2,
            because: "a grant is for what it was granted for — approving one path is not "
                + "approving the next one");
    }
}
