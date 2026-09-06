// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Standard.Agents.Brokers.Skills;
using Standard.Agents.Models.Foundations.Skills;

namespace Standard.Agents.Evals.Live;

// Skills from the PeerLLM registry (skills.peerllm.com), pinned to a skillset version, through
// the same ISkillBroker seam the built-in file skills use. The live evals carry this thin
// broker rather than the Standard.Agents.Data.Skills.PeerLLM package because that package pins
// an older core; when it moves to 2.x, this file goes.
public sealed class PeerLLMRegistrySkillBroker : ISkillBroker
{
    private const string BaseUrl = "https://skills.peerllm.com/api/v1/";

    private readonly HttpClient httpClient;
    private readonly string skillset;
    private readonly IReadOnlyList<string> members;

    public PeerLLMRegistrySkillBroker(
        HttpClient httpClient,
        string skillset,
        IReadOnlyList<string> members)
    {
        this.httpClient = httpClient;
        this.httpClient.BaseAddress ??= new Uri(BaseUrl);
        this.skillset = skillset.Trim('/');
        this.members = members;
    }

    public int ResolvedVersion { get; private set; }

    public async ValueTask<IReadOnlyList<Skill>> SelectSkillsAsync()
    {
        SkillsetDto set =
            await this.httpClient.GetFromJsonAsync<SkillsetDto>($"skillsets/{this.skillset}")
                ?? new SkillsetDto();

        this.ResolvedVersion = set.Version;
        List<Skill> skills = [];

        foreach (MemberDto member in set.Members)
        {
            if (this.members.Count > 0 && this.members.Contains(member.Name) is false)
            {
                continue;
            }

            SkillDto? skill = await this.httpClient.GetFromJsonAsync<SkillDto>(
                $"skills/id/{member.SkillId}@{member.Version}");

            if (skill is not null)
            {
                skills.Add(new Skill
                {
                    Name = skill.Name,
                    Description = skill.Description,
                    Content = StripFrontmatter(skill.SkillMd)
                });
            }
        }

        return skills;
    }

    // The registry serves SKILL.md whole; the YAML frontmatter is the registry's metadata, not
    // instructions for the model.
    private static string StripFrontmatter(string skillMd)
    {
        if (skillMd.StartsWith("---", StringComparison.Ordinal) is false)
        {
            return skillMd;
        }

        int end = skillMd.IndexOf("\n---", 3, StringComparison.Ordinal);

        return end < 0 ? skillMd : skillMd[(end + 4)..].TrimStart('\r', '\n');
    }

    private sealed record SkillsetDto
    {
        [JsonPropertyName("version")] public int Version { get; init; }
        [JsonPropertyName("members")] public MemberDto[] Members { get; init; } = [];
    }

    private sealed record MemberDto
    {
        [JsonPropertyName("skill_id")] public string SkillId { get; init; } = string.Empty;
        [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
        [JsonPropertyName("version")] public int Version { get; init; }
    }

    private sealed record SkillDto
    {
        [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
        [JsonPropertyName("description")] public string Description { get; init; } = string.Empty;
        [JsonPropertyName("skill_md")] public string SkillMd { get; init; } = string.Empty;
    }
}
