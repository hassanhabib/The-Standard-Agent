// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace Standard.Agents.Tests.Unit.Packaging;

// README.md is not only documentation. It is packed as PackageReadmeFile, so it IS the NuGet
// package page — the first thing anyone considering the library reads.
//
// NuGet's markdown pipeline is not GitHub's. It renders raw HTML as literal text, so a
// <div align="center"> wrapper that centres the title on GitHub prints the angle brackets on
// NuGet, and the package page opened with a stray closing tag for several releases before anyone
// looked. It also has no repository context, so a relative link resolves to nothing.
//
// Neither failure is visible from the repo, which is exactly why it is a test: the README renders
// correctly on GitHub the whole time it is broken on the page that sells the package.
public class PackageReadmeTests
{
    private static readonly string readmeText =
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "README.md"));

    // HTML inside a fence is shown AS code on both renderers, which is correct and intended —
    // the structure block and the C# samples are full of angle brackets. Only prose is at risk.
    private static string Prose()
    {
        string withoutFences = Regex.Replace(readmeText, "```.*?```", string.Empty,
            RegexOptions.Singleline);

        return Regex.Replace(withoutFences, "`[^`\n]*`", string.Empty);
    }

    [Fact]
    public void ShouldCarryNoRawHtmlSoTheNuGetPageDoesNotPrintTheTags()
    {
        // given
        string prose = Prose();

        // when
        string[] tags = [.. Regex.Matches(prose, "</?[a-zA-Z][^>]*>").Select(match => match.Value)];

        // then
        tags.Should().BeEmpty(
            because: "README.md is the NuGet package page, and NuGet renders raw HTML as literal "
                + $"text — [{string.Join(", ", tags)}] would print on the page as written. "
                + "Markdown has no centring, so there is nothing to swap it for: state it in "
                + "markdown, or accept that it reads left-aligned.");
    }

    // The package page has no repository to resolve against, so every target must be absolute.
    // Anchors are fine: they resolve within the rendered page itself.
    [Fact]
    public void ShouldLinkAbsolutelySoEveryTargetResolvesOffTheRepository()
    {
        // given
        string prose = Prose();

        // when
        string[] relativeTargets =
            [.. Regex.Matches(prose, @"\]\(([^)]+)\)")
                .Select(match => match.Groups[1].Value)
                .Where(target => Regex.IsMatch(target, "^(https?://|#)") is false)];

        // then
        relativeTargets.Should().BeEmpty(
            because: "a relative target resolves against the repository, and the NuGet page has "
                + $"no repository — [{string.Join(", ", relativeTargets)}] would be a dead link "
                + "for every reader who arrives at the package rather than the repo.");
    }
}
