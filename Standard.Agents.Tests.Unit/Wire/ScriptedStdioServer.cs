// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Text.Json.Nodes;
using System.Threading.Channels;

namespace Standard.Agents.Tests.Unit.Wire;

// A protocol server on the other end of a process's standard streams, in memory: every line the
// broker writes is recorded, and every line the script answers is queued for the broker to read.
// The script may answer a line with nothing (a notification), or with several lines (a server
// that speaks before it answers).
internal sealed class ScriptedStdioServer
{
    private readonly Channel<string?> serverLines = Channel.CreateUnbounded<string?>();
    private readonly Func<JsonNode, IEnumerable<string>> respond;

    public ScriptedStdioServer(Func<JsonNode, IEnumerable<string>> respond)
    {
        this.respond = respond;
        this.Output = new ChannelReader(this.serverLines.Reader);
        this.Input = new RecordingWriter(this);
    }

    public TextReader Output { get; }
    public TextWriter Input { get; }
    public List<string> Lines { get; } = [];

    public void End() =>
        this.serverLines.Writer.TryWrite(null);

    private void Receive(string line)
    {
        this.Lines.Add(line);

        foreach (string answer in this.respond(JsonNode.Parse(line)!))
        {
            this.serverLines.Writer.TryWrite(answer);
        }
    }

    private sealed class ChannelReader : TextReader
    {
        private readonly ChannelReader<string?> lines;

        public ChannelReader(ChannelReader<string?> lines) =>
            this.lines = lines;

        public override async ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken) =>
            await this.lines.ReadAsync(cancellationToken);

        public override Task<string?> ReadLineAsync() =>
            ReadLineAsync(CancellationToken.None).AsTask();
    }

    private sealed class RecordingWriter : TextWriter
    {
        private readonly ScriptedStdioServer server;

        public RecordingWriter(ScriptedStdioServer server) =>
            this.server = server;

        public override System.Text.Encoding Encoding => System.Text.Encoding.UTF8;

        public override void Write(char value) =>
            throw new NotSupportedException("The broker writes whole lines.");

        public override Task WriteLineAsync(string? value)
        {
            this.server.Receive(value ?? string.Empty);

            return Task.CompletedTask;
        }

        public override Task FlushAsync() =>
            Task.CompletedTask;
    }
}
