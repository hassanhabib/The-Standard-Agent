// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace Standard.Agents.Tests.Unit.Packaging;

// An endpoint written in the documentation is an endpoint someone will paste. hosting.md carried
// a base that already named chat/completions for several releases, and a copied example reached
// v1/chat/chat/completions (principal review 2026-09-04, F-12). The docs are copied next to the
// assembly so every documented endpoint is composed through the real verb: a documented URL the
// builder refuses is a documentation bug, caught here rather than by the next reader.
public class DocumentedApiUrlTests
{
    private static readonly string[] documentedFiles =
    [
        "README.md",
        Path.Combine("docs", "hosting.md"),
        Path.Combine("docs", "how-to.md")
    ];

    // The three shapes an endpoint takes in the docs: a C# argument, a JSON agent document, and
    // the host's appsettings entry.
    private static readonly Regex documentedApiUrl = new(
        "(?:\"?apiUrl\"?|\"Url\")\\s*:\\s*\"(?<url>https?://[^\"]+)\"",
        RegexOptions.Compiled);

    public static TheoryData<string, string> DocumentedApiUrls()
    {
        var documentedApiUrls = new TheoryData<string, string>();

        foreach (string documentedFile in documentedFiles)
        {
            string documentation =
                File.ReadAllText(Path.Combine(AppContext.BaseDirectory, documentedFile));

            foreach (Match match in documentedApiUrl.Matches(documentation))
            {
                documentedApiUrls.Add(documentedFile, match.Groups["url"].Value);
            }
        }

        return documentedApiUrls;
    }

    [Theory]
    [MemberData(nameof(DocumentedApiUrls))]
    public void ShouldComposeEveryDocumentedApiUrl(string documentedFile, string documentedApiUrl)
    {
        // given . when
        Action brainAction = () =>
            new StandardAgent().Brain(apiUrl: documentedApiUrl, apiKey: "key", model: "model");

        // then
        brainAction.Should().NotThrow(
            because: $"{documentedFile} teaches this endpoint, and a copied example must compose");
    }
}
