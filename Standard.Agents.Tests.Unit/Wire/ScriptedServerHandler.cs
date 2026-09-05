// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Net;
using System.Text;

namespace Standard.Agents.Tests.Unit.Wire;

// A protocol server in a handler: records every request the broker sends, byte for byte, and
// answers from a script. Deterministic, in-process, no port - the wire-contract tests run the
// real brokers against it (principal review 2026-09-04, F-21).
internal sealed class ScriptedServerHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, string, CancellationToken, Task<HttpResponseMessage>>
        respondAsync;

    public ScriptedServerHandler(Func<HttpRequestMessage, string, HttpResponseMessage> respond)
        : this(async (request, body, _) => respond(request, body))
    {
    }

    public ScriptedServerHandler(
        Func<HttpRequestMessage, string, CancellationToken, Task<HttpResponseMessage>> respondAsync) =>
        this.respondAsync = respondAsync;

    public List<HttpRequestMessage> Requests { get; } = [];
    public List<string> Bodies { get; } = [];

    public static ScriptedServerHandler Answering(string body, string mediaType = "application/json") =>
        new((_, _) => Json(HttpStatusCode.OK, body, mediaType));

    public static ScriptedServerHandler AnsweringWith(HttpStatusCode statusCode, string body = "") =>
        new((_, _) => Json(statusCode, body));

    // Holds the connection open until the caller gives up - a provider that never answers.
    public static ScriptedServerHandler Hanging() =>
        new(async (_, _, cancellationToken) =>
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);

            return Json(HttpStatusCode.OK, "{}");
        });

    public static HttpResponseMessage Json(
        HttpStatusCode statusCode,
        string body,
        string mediaType = "application/json") =>
        new(statusCode)
        {
            Content = new StringContent(body, Encoding.UTF8, mediaType)
        };

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        string body = request.Content is null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken);

        this.Requests.Add(request);
        this.Bodies.Add(body);

        return await this.respondAsync(request, body, cancellationToken);
    }
}
