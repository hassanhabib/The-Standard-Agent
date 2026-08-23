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

    // Gap 5, found in the 2026-08-23 sweep. PermissionMode.Deny was declared, documented
    // ("Denied. Nothing runs but what was named.") and never consulted: the only read of the
    // mode compared against Ask, so the strictest disposition behaved exactly like Open and an
    // unnamed tool ran unasked.
    [Fact]
    public async Task ShouldDenyAnActThatWasNeverExplicitlyPermittedAsync()
    {
        // given — nothing names the tool: no allow-list, no approval list, no policy
        var tool = new WriteFileTool();
        string secondPrompt = string.Empty;
        int asked = 0;

        StandardAgent agent = new StandardAgent()
            .Tool(tool)
            .OnBrain((_, userPrompt) =>
            {
                if (asked > 0)
                {
                    secondPrompt = userPrompt;
                }

                string reply = asked == 0 ? "ACTION: write_file: /project/a.txt hello" : "FINAL: done";
                asked++;

                return ValueTask.FromResult(reply);
            })
            .Permissions(PermissionMode.Deny)
            .MaxTurns(4);

        // when
        string actualAnswer = await agent.ProcessPromptAsync("write it");

        // then — denied, told, and non-terminal: the agent chooses another path
        tool.Writes.Should().Be(
            0,
            because: "Deny says nothing runs but what was named, and nothing named this tool");

        secondPrompt.Should().Contain(
            "not permitted",
            because: "a denial the agent is not told about leaves it to propose the act forever");

        actualAnswer.Should().Be("done");
    }

    // The positive half, so a mode that denies everything cannot pass: an act that WAS named —
    // here through RequireApproval — still reaches its authority and still runs when permitted.
    [Fact]
    public async Task ShouldStillPermitWhatWasExplicitlyNamedUnderDenyAsync()
    {
        // given
        var tool = new WriteFileTool();

        StandardAgent agent = AgentActing(tool, "ACTION: write_file: /project/a.txt hello", "FINAL: done")
            .Permissions(PermissionMode.Deny)
            .RequireApproval("write_file")
            .OnApproval(_ => ValueTask.FromResult(ApprovalDecision.Approved));

        // when
        await agent.ProcessPromptAsync("write it");

        // then — the mode speaks only for what the explicit permissions did not mention
        tool.Writes.Should().Be(1);
    }

    // Gap 6, found in the 2026-08-23 sweep. Ask promises "held, not failed, exactly as
    // RequireApproval does" — and RequireApproval with no approver holds, because waiting is
    // not consent. But Ask alone routed to NotConfiguredApprovalBroker, which answered
    // Approved unconditionally: the perimeter asked, heard yes from nobody, and ran the act.
    [Fact]
    public async Task ShouldHoldAnUnpermittedActWhenAskHasNoAuthorityAsync()
    {
        // given — Ask, and nobody wired to answer
        var tool = new WriteFileTool();

        StandardAgent agent = AgentActing(tool, "ACTION: write_file: /project/a.txt hello", "FINAL: done")
            .Permissions(PermissionMode.Ask);

        // when
        string actualAnswer = await agent.ProcessPromptAsync("write it");

        // then — held, not performed: an authority that does not exist cannot have said yes
        tool.Writes.Should().Be(
            0,
            because: "Ask with no approval authority must hold the act — waiting is not consent, "
                + "and an absent authority is nothing but waiting");

        actualAnswer.Should().Contain("waiting for approval");
    }

    // The same silent consent through the second door: RequireApprovalBroker answered Approved
    // for any tool NOT on its list — a branch only reachable under Ask, where it meant an act
    // the authority was never told about ran as if someone had said yes.
    [Fact]
    public async Task ShouldHoldAnUnlistedActUnderAskWhenTheAuthorityOnlyKnowsOtherToolsAsync()
    {
        // given — the approval list names a different tool entirely
        var tool = new WriteFileTool();

        StandardAgent agent = AgentActing(tool, "ACTION: write_file: /project/a.txt hello", "FINAL: done")
            .Permissions(PermissionMode.Ask)
            .RequireApproval("wire_transfer");

        // when
        string actualAnswer = await agent.ProcessPromptAsync("write it");

        // then
        tool.Writes.Should().Be(
            0,
            because: "the list names wire_transfer, nobody can answer for write_file, and an "
                + "unanswerable question is a held act, not a granted one");

        actualAnswer.Should().Contain("waiting for approval");
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

    // Gap 7, found in the 2026-08-23 sweep. The grant key is the tool AND the scope, exactly —
    // but ScopeOf defaults to empty, so for any tool that names no scope (every MCP tool among
    // them) the key collapsed to the tool name alone: approving a $10 transfer silently
    // approved the $10,000 one later in the same run. An act with no named scope cannot be
    // matched to a later one "exactly", so nothing may be remembered and each act is asked
    // about — an identical repeat is already replayed by run-once before approval is reached.
    [Fact]
    public async Task ShouldAskAgainWhenTheToolNamesNoScopeAsync()
    {
        // given — a tool with no ScopeOf, proposing two very different acts
        var tool = new WireTransferTool();
        int asked = 0;

        StandardAgent agent = AgentActing(
            tool,
            "ACTION: wire_transfer: 10 to bob",
            "ACTION: wire_transfer: 10000 to mallory",
            "FINAL: done")
                .RequireApproval("wire_transfer")
                .OnApproval(_ =>
                {
                    asked++;

                    return ValueTask.FromResult(ApprovalDecision.Approved);
                });

        // when
        await agent.ProcessPromptAsync("settle up");

        // then
        asked.Should().Be(
            2,
            because: "approving ten dollars is not approving ten thousand — with no scope to "
                + "match exactly, every act of the tool is its own question");

        tool.Transfers.Should().Be(2);
    }

    private sealed class WireTransferTool : ITool
    {
        public string Name => "wire_transfer";
        public string Description => "Moves money.";
        public int Transfers { get; private set; }

        // Deliberately no ScopeOf: this is the shape of every tool that does not implement it,
        // which includes every tool that arrives over MCP.
        public ValueTask<string> ExecuteAsync(string input)
        {
            Transfers++;

            return ValueTask.FromResult("paid");
        }
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
