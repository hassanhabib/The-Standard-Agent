// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Brokers.Knowledges;
using Standard.Agents.Brokers.Memorys;
using Standard.Agents.Brokers.Skills;
using Standard.Agents.Models.Brokers.Generators.V1;
using Standard.Agents.Models.Foundations.Skills;
using Standard.Agents.Tools;
using Xunit;

namespace Standard.Agents.Tests.Unit.Acceptance;

// Selection (SPEC.md §4.15): what an agent CARRIES and what a run is OFFERED are different
// things. A selector names the subset of described tools the run is offered; the Brain decides
// among what was offered; the perimeter never moves. A greeting is offered nothing, and a
// model cannot over-call a tool it was never shown.
public class SelectionTests
{
    private sealed class NamedTool : ITool
    {
        public NamedTool(string name) => Name = name;

        public string Name { get; }
        public string Description => $"The {Name} tool.";
        public string Parameters => """{"type":"object"}""";
        public int Executions { get; private set; }

        public ValueTask<string> ExecuteAsync(string input)
        {
            Executions++;

            return ValueTask.FromResult("ran");
        }
    }

    private static StandardAgent AgentWith(
        Func<IReadOnlyList<ToolDefinition>, GenerationResult> reply,
        params ITool[] tools)
    {
        var skillBroker = new Mock<ISkillBroker>();

        skillBroker.Setup(broker => broker.SelectSkillsAsync())
            .ReturnsAsync(new List<Skill> { new() { Content = "You are an assistant." } });

        var memoryBroker = new Mock<IMemoryBroker>();
        memoryBroker.Setup(broker => broker.SelectMemoriesAsync()).ReturnsAsync([]);

        var knowledgeBroker = new Mock<IKnowledgeBroker>();

        knowledgeBroker.Setup(broker => broker.SelectKnowledgeAsync(It.IsAny<string>()))
            .ReturnsAsync([]);

        StandardAgent agent = new StandardAgent()
            .UseSkills(skillBroker.Object)
            .UseMemory(memoryBroker.Object)
            .UseKnowledge(knowledgeBroker.Object)
            .OnNativeBrain((messages, tools) => ValueTask.FromResult(reply(tools)));

        foreach (ITool tool in tools)
        {
            agent = agent.Tool(tool);
        }

        return agent;
    }

    private static StandardAgent TextAgentWith(
        Func<string, string, ValueTask<string>> brain,
        params ITool[] tools)
    {
        var skillBroker = new Mock<ISkillBroker>();

        skillBroker.Setup(broker => broker.SelectSkillsAsync())
            .ReturnsAsync(new List<Skill>
            {
                new() { Content = "You are an assistant.\n\nTools:\n{{tools}}" },
            });

        var memoryBroker = new Mock<IMemoryBroker>();
        memoryBroker.Setup(broker => broker.SelectMemoriesAsync()).ReturnsAsync([]);

        var knowledgeBroker = new Mock<IKnowledgeBroker>();

        knowledgeBroker.Setup(broker => broker.SelectKnowledgeAsync(It.IsAny<string>()))
            .ReturnsAsync([]);

        StandardAgent agent = new StandardAgent()
            .UseSkills(skillBroker.Object)
            .UseMemory(memoryBroker.Object)
            .UseKnowledge(knowledgeBroker.Object)
            .OnBrain(brain);

        foreach (ITool tool in tools)
        {
            agent = agent.Tool(tool);
        }

        return agent;
    }

    [Fact]
    public async Task ShouldExpandTheToolsMarkerWithOnlyTheSelectedToolsAsync()
    {
        // given — a text-protocol agent advertising through the {{tools}} marker
        string seenSystemPrompt = null!;

        StandardAgent agent = TextAgentWith(
            (systemPrompt, _) =>
            {
                seenSystemPrompt = systemPrompt;

                return ValueTask.FromResult("FINAL: ok");
            },
            new NamedTool("web_search"),
            new NamedTool("code_search"))
            .OnSelectTools((task, described) =>
                new ValueTask<IReadOnlyList<string>>(new[] { "web_search" }));

        // when
        await agent.ProcessPromptAsync("look something up");

        // then — the catalog the Brain reads lists the offered tool and nothing else
        seenSystemPrompt.Should().Contain("web_search");
        seenSystemPrompt.Should().NotContain("code_search");
    }

    [Fact]
    public async Task ShouldOfferTheNativeBrainOnlyWhatTheSelectorNamedAsync()
    {
        // given — an agent carrying two described tools, and a selector that judges only one
        // relevant to this task
        IReadOnlyList<ToolDefinition> shown = [];

        StandardAgent agent = AgentWith(
            tools =>
            {
                shown = tools;

                return new GenerationResult { Content = "hello!" };
            },
            new NamedTool("web_search"),
            new NamedTool("code_search"))
            .OnSelectTools((task, described) =>
                new ValueTask<IReadOnlyList<string>>(new[] { "web_search" }));

        // when
        string answer = await agent.ProcessPromptAsync("look something up");

        // then — the run was offered the selected tool and nothing else
        answer.Should().Be("hello!");
        shown.Should().Contain(definition => definition.Name == "web_search");
        shown.Should().NotContain(definition => definition.Name == "code_search");
    }
}
